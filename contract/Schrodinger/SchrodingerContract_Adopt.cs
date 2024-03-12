using System.Collections.Generic;
using System.Linq;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Points.Contracts.Point;

namespace Schrodinger;

public partial class SchrodingerContract
{
    public override Empty Adopt(AdoptInput input)
    {
        CheckInitialized();
        ValidateAdoptInput(input);

        var tick = GetTickFromSymbol(input.Parent);
        var inscriptionInfo = State.InscriptionInfoMap[tick];
        Assert(inscriptionInfo != null, "Tick not deployed.");

        var adoptId = GenerateAdoptId(tick);
        Assert(State.AdoptInfoMap[adoptId] == null, "Adopt id already exists.");

        GetParentInfo(inscriptionInfo, input.Parent, out var parentGen, out var parentAttributes);

        Assert(parentGen < inscriptionInfo.MaxGen, "Exceeds max gen.");

        var adoptInfo = new AdoptInfo
        {
            AdoptId = adoptId,
            Parent = input.Parent,
            ParentGen = parentGen,
            ParentAttributes = parentAttributes,
            BlockHeight = Context.CurrentHeight,
            Adopter = Context.Sender,
            ImageCount = inscriptionInfo.ImageCount
        };

        State.AdoptInfoMap[adoptId] = adoptInfo;

        CalculateAmount(inscriptionInfo, input.Amount, out var lossAmount, out var commissionAmount,
            out var outputAmount);
        adoptInfo.InputAmount = input.Amount;
        adoptInfo.OutputAmount = outputAmount;

        ProcessToken(input.Parent, input.Amount, parentGen == 0 ? lossAmount : input.Amount, commissionAmount,
            inscriptionInfo.Recipient, inscriptionInfo.Ancestor);

        var randomHash = GetRandomHash();
        adoptInfo.Gen = GenerateGen(inscriptionInfo, parentGen, randomHash);
        adoptInfo.Attributes = GenerateAttributes(parentAttributes, tick, inscriptionInfo.AttributesPerGen,
            adoptInfo.Gen.Sub(adoptInfo.ParentGen), randomHash);
        adoptInfo.Symbol = GenerateSymbol(tick);
        adoptInfo.TokenName = GenerateTokenName(adoptInfo.Symbol, adoptInfo.Gen);

        JoinPointsContract(input.Domain);
        SettlePointsContract(nameof(Adopt));

        Context.Fire(new Adopted
        {
            AdoptId = adoptId,
            Parent = input.Parent,
            ParentGen = parentGen,
            InputAmount = input.Amount,
            LossAmount = lossAmount,
            CommissionAmount = commissionAmount,
            OutputAmount = outputAmount,
            ImageCount = inscriptionInfo.ImageCount,
            Adopter = Context.Sender,
            BlockHeight = Context.CurrentHeight,
            Attributes = adoptInfo.Attributes,
            Gen = adoptInfo.Gen,
            Ancestor = inscriptionInfo.Ancestor,
            Symbol = adoptInfo.Symbol,
            TokenName = adoptInfo.TokenName
        });

        return new Empty();
    }

    private void ValidateAdoptInput(AdoptInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(IsSymbolValid(input.Parent), "Invalid input parent.");
        Assert(input.Amount > 0, "Invalid input amount.");
        Assert(IsStringValid(input.Domain), "Invalid input domain.");
    }

    private Hash GenerateAdoptId(string tick)
    {
        return HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(tick),
            HashHelper.ComputeFrom(State.SymbolCountMap[tick]));
    }

    private void GetParentInfo(InscriptionInfo inscriptionInfo, string parentSymbol, out int parentGen,
        out Attributes parentAttributes)
    {
        // gen0
        if (parentSymbol == inscriptionInfo.Ancestor)
        {
            parentGen = 0;
            parentAttributes = new Attributes();
        }
        else
        {
            var adoptId = State.SymbolAdoptIdMap[parentSymbol];
            Assert(adoptId != null, $"{parentSymbol} not exists.");

            var parentInfo = State.AdoptInfoMap[adoptId];
            parentGen = parentInfo.Gen;
            parentAttributes = parentInfo.Attributes;
        }
    }

    private void CalculateAmount(InscriptionInfo inscriptionInfo, long inputAmount, out long lossAmount,
        out long commissionAmount, out long outputAmount)
    {
        // calculate amount
        lossAmount = inputAmount.Mul(inscriptionInfo.LossRate).Div(SchrodingerContractConstants.Denominator);
        if (lossAmount == 0) lossAmount = lossAmount.Add(1);

        outputAmount = inputAmount.Sub(lossAmount);

        commissionAmount = lossAmount.Mul(inscriptionInfo.CommissionRate).Div(SchrodingerContractConstants.Denominator);
        if (commissionAmount == 0) commissionAmount++;

        lossAmount = lossAmount.Sub(commissionAmount);
    }

    private void ProcessToken(string symbol, long inputAmount, long burnAmount, long outputAmount, Address to,
        string outputSymbol)
    {
        // transfer parent from sender
        State.TokenContract.TransferFrom.Send(new TransferFromInput
        {
            Amount = inputAmount,
            From = Context.Sender,
            To = Context.Self,
            Symbol = symbol
        });

        if (burnAmount > 0)
        {
            // burn token
            State.TokenContract.Burn.Send(new BurnInput
            {
                Symbol = symbol,
                Amount = burnAmount
            });
        }

        // send commission to recipient
        State.TokenContract.Transfer.Send(new TransferInput
        {
            Amount = outputAmount,
            To = to,
            Symbol = outputSymbol
        });
    }

    private Hash GetRandomHash()
    {
        if (State.ConsensusContract.Value == null)
        {
            State.ConsensusContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
        }

        return State.ConsensusContract.GetRandomHash.Call(new Int64Value
        {
            Value = Context.CurrentHeight
        });
    }

    private int GenerateGen(InscriptionInfo inscriptionInfo, int parentGen, Hash randomHash)
    {
        var crossGenerationConfig = inscriptionInfo.CrossGenerationConfig;

        parentGen++;

        if (crossGenerationConfig.Gen == 0 ||
            !IsCrossGenerationHappened(crossGenerationConfig.CrossGenerationProbability, randomHash))
        {
            return parentGen;
        }

        if (crossGenerationConfig.CrossGenerationFixed)
        {
            var newGen = parentGen + crossGenerationConfig.Gen;
            return newGen >= inscriptionInfo.MaxGen ? inscriptionInfo.MaxGen : newGen;
        }

        var gens = Enumerable.Range(1, crossGenerationConfig.Gen).Select(g => new ItemWithWeight
        {
            Item = g.ToString(),
            Weight = ReverseWeight(crossGenerationConfig.Weights[g - 1])
        }).ToList();

        var selected = int.TryParse(GetRandomItems(randomHash, nameof(GenerateGen), gens, 1).FirstOrDefault(),
            out var gen);

        var result = parentGen.Add(selected ? gen : 0);
        return result >= inscriptionInfo.MaxGen ? inscriptionInfo.MaxGen : result;
    }

    private Hash CalculateRandomHash(Hash randomHash, string seed)
    {
        return HashHelper.ConcatAndCompute(randomHash, HashHelper.ComputeFrom(seed));
    }

    private long ReverseWeight(long weight)
    {
        return SchrodingerContractConstants.DefaultMaxAttributeWeight.Sub(weight);
    }

    private bool IsCrossGenerationHappened(long probability, Hash randomHash)
    {
        var random = Context.ConvertHashToInt64(CalculateRandomHash(randomHash, nameof(IsCrossGenerationHappened)), 0,
            SchrodingerContractConstants.Denominator);
        return random <= probability;
    }

    private List<string> GetRandomItems(Hash randomHash, string seed, List<ItemWithWeight> items, int count)
    {
        var selectedItems = new List<string>();
        var totalWeights = items.Select(i => i.Weight).Sum();

        while (selectedItems.Count < count && items.Count > 0)
        {
            var random = Context.ConvertHashToInt64(CalculateRandomHash(randomHash, seed), 0, totalWeights);
            var sum = 0L;
            for (var i = 0; i < items.Count; i++)
            {
                sum = sum.Add(items[i].Weight);

                if (random >= sum) continue;

                selectedItems.Add(items[i].Item);
                totalWeights = totalWeights.Add(items[i].Weight);
                items.RemoveAt(i);
                break;
            }
        }

        return selectedItems;
    }

    private Attributes GenerateAttributes(Attributes parentAttributes, string tick, int attributesPerGen, int amount,
        Hash randomHash)
    {
        var attributes = new Attributes();

        // gen0 -> gen1
        if (parentAttributes.Data.Count == 0)
        {
            attributes.Data.AddRange(State.FixedTraitTypeMap[tick].Data.Select(t => new Attribute
            {
                TraitType = t.Name,
                Value = GetRandomItems(randomHash, nameof(t.Name),
                        State.TraitValueMap[tick][t.Name].Data.Select(a => new ItemWithWeight
                        {
                            Item = a.Name,
                            Weight = ReverseWeight(a.Weight)
                        }).ToList(), 1)
                    .FirstOrDefault()
            }));

            amount = amount.Sub(1);
        }
        else
        {
            attributes.Data.AddRange(parentAttributes.Data);
        }

        // get non-selected trait types
        var traitTypes = State.RandomTraitTypeMap[tick].Data
            .Where(a => attributes.Data.Select(t => t.TraitType).All(t => t != a.Name)).ToList();

        // select trait types randomly
        var randomTraitTypes = GetRandomItems(randomHash, nameof(GenerateAttributes),
            traitTypes.Select(t => new ItemWithWeight
            {
                Item = t.Name,
                Weight = ReverseWeight(t.Weight)
            }).ToList(), amount);

        // select trait values randomly
        attributes.Data.AddRange(randomTraitTypes.Select(t => new Attribute
        {
            TraitType = t,
            Value = GetRandomItems(randomHash, nameof(t),
                State.TraitValueMap[tick][t].Data.Select(a => new ItemWithWeight
                {
                    Item = a.Name,
                    Weight = ReverseWeight(a.Weight)
                }).ToList(), 1).FirstOrDefault()
        }));

        return attributes;
    }

    public override Empty Confirm(ConfirmInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(input.AdoptId != null, "Invalid input adopt id.");
        Assert(IsByteStringValid(input.Signature), "Invalid input signature.");

        CheckImageSize(input.Image);

        var adoptInfo = State.AdoptInfoMap[input.AdoptId];
        Assert(adoptInfo != null, "Adopt id not exists.");
        Assert(adoptInfo.Adopter == Context.Sender, "No permission.");

        Assert(!adoptInfo.IsConfirmed, "Adopt id already confirmed.");
        Assert(RecoverAddressFromSignature(input) == State.Config.Value.Signatory, "Not authorized.");

        adoptInfo.IsConfirmed = true;
        State.SymbolAdoptIdMap[adoptInfo.Symbol] = adoptInfo.AdoptId;

        var tick = GetTickFromSymbol(adoptInfo.Parent);
        var inscriptionInfo = State.InscriptionInfoMap[tick];

        var externalInfo = GenerateAdoptExternalInfo(tick, input.Image, adoptInfo.OutputAmount, adoptInfo.Gen,
            adoptInfo.Attributes);

        CreateInscriptionAndIssue(adoptInfo.Symbol, adoptInfo.TokenName, inscriptionInfo.Decimals,
            adoptInfo.OutputAmount, externalInfo, Context.Self, Context.Self);

        Context.Fire(new Confirmed
        {
            AdoptId = input.AdoptId,
            Parent = adoptInfo.Parent,
            Symbol = adoptInfo.Symbol,
            TotalSupply = adoptInfo.OutputAmount,
            Attributes = adoptInfo.Attributes,
            Decimals = inscriptionInfo.Decimals,
            Deployer = Context.Sender,
            Gen = adoptInfo.Gen,
            Issuer = Context.Self,
            Owner = Context.Self,
            IssueChainId = Context.ChainId,
            TokenName = adoptInfo.TokenName,
            ExternalInfos = new ExternalInfos
            {
                Value = { externalInfo.Value }
            }
        });

        return new Empty();
    }

    private Address RecoverAddressFromSignature(ConfirmInput input)
    {
        var hash = ComputeConfirmInputHash(input);
        var publicKey = Context.RecoverPublicKey(input.Signature.ToByteArray(), hash.ToByteArray());
        Assert(publicKey != null, "Invalid signature.");

        return Address.FromPublicKey(publicKey);
    }

    private Hash ComputeConfirmInputHash(ConfirmInput input)
    {
        return HashHelper.ComputeFrom(new ConfirmInput
        {
            AdoptId = input.AdoptId,
            Image = input.Image
        }.ToByteArray());
    }

    private string GenerateSymbol(string tick)
    {
        return $"{tick}{SchrodingerContractConstants.Separator}{State.SymbolCountMap[tick]++}";
    }

    private string GenerateTokenName(string symbol, int gen)
    {
        return $"{symbol}{SchrodingerContractConstants.TokenNameSuffix}{gen.ToString()}";
    }

    private ExternalInfo GenerateAdoptExternalInfo(string tick, string image, long totalSupply, int gen,
        Attributes attributes)
    {
        var externalInfo = new ExternalInfo();
        var dic = new Dictionary<string, string>
        {
            [SchrodingerContractConstants.InscriptionImageKey] = image
        };

        var info = new AdoptInscriptionInfo
        {
            P = SchrodingerContractConstants.InscriptionType,
            Op = SchrodingerContractConstants.AdoptOp,
            Tick = tick,
            Amt = SchrodingerContractConstants.Amt,
            Gen = gen.ToString()
        };
        dic[SchrodingerContractConstants.InscriptionAdoptKey] = info.ToString();

        dic[SchrodingerContractConstants.AttributesKey] = attributes.Data.ToString();

        externalInfo.Value.Add(dic);
        return externalInfo;
    }

    private void CreateInscriptionAndIssue(string symbol, string tokenName, int decimals, long totalSupply,
        ExternalInfo externalInfo, Address issuer, Address owner)
    {
        State.TokenContract.Create.Send(new CreateInput
        {
            Symbol = symbol,
            TokenName = tokenName,
            Decimals = decimals,
            IsBurnable = true,
            Issuer = issuer,
            TotalSupply = totalSupply,
            Owner = owner,
            IssueChainId = Context.ChainId,
            ExternalInfo = externalInfo
        });

        State.TokenContract.Issue.Send(new IssueInput
        {
            To = Context.Sender,
            Symbol = symbol,
            Amount = totalSupply
        });
    }

    public override Empty Reset(ResetInput input)
    {
        CheckInitialized();

        Assert(input != null, "Invalid input.");
        Assert(IsSymbolValid(input.Symbol), "Invalid input symbol.");
        Assert(input.Amount > 0, "Invalid input amount.");
        Assert(IsStringValid(input.Domain), "Invalid input domain.");

        var tick = GetTickFromSymbol(input.Symbol);
        var inscriptionInfo = State.InscriptionInfoMap[tick];

        Assert(inscriptionInfo != null, "Tick not deployed.");
        Assert(inscriptionInfo.Ancestor != input.Symbol, "Can not reset gen0.");

        ProcessToken(input.Symbol, input.Amount, input.Amount, input.Amount, Context.Sender,
            inscriptionInfo.Ancestor);

        JoinPointsContract(input.Domain);
        SettlePointsContract(nameof(Reset));

        Context.Fire(new TokenReset
        {
            Symbol = input.Symbol,
            Ancestor = inscriptionInfo.Ancestor,
            Amount = input.Amount,
            Recipient = Context.Sender
        });

        return new Empty();
    }

    private void JoinPointsContract(string domain)
    {
        if (State.JoinRecord[Context.Sender]) return;

        State.JoinRecord[Context.Sender] = true;

        State.PointsContract.Join.Send(new JoinInput
        {
            DappId = State.PointsContractDAppId.Value,
            Domain = domain,
            Registrant = Context.Sender
        });
    }

    private void SettlePointsContract(string actionName)
    {
        State.PointsContract.Settle.Send(new SettleInput
        {
            DappId = State.PointsContractDAppId.Value,
            ActionName = actionName,
            UserAddress = Context.Sender
        });
    }
}