using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;

namespace Schrodinger;

public partial class SchrodingerContract
{
    private void CheckAdminPermission()
    {
        Assert(Context.Sender == State.Admin.Value, "No permission.");
    }

    private void CheckInitialized()
    {
        Assert(State.Initialized.Value, "Not initialized.");
    }

    private bool IsAddressValid(Address input)
    {
        return input != null && !input.Value.IsNullOrEmpty();
    }

    private bool IsHashValid(Hash input)
    {
        return input != null && !input.Value.IsNullOrEmpty();
    }

    private bool IsStringValid(string input)
    {
        return !string.IsNullOrWhiteSpace(input);
    }

    private bool IsByteStringValid(ByteString input)
    {
        return input != null && input.Length > 0;
    }

    private bool IsSymbolValid(string input)
    {
        return IsStringValid(input) && input.Split(SchrodingerContractConstants.Separator).Length == 2;
    }

    private string GetTickFromSymbol(string symbol)
    {
        return symbol.Split(SchrodingerContractConstants.Separator)[0].ToUpper();
    }

    #region Deploy

    private ExternalInfo GenerateExternalInfo(string tick, string image, long totalSupply)
    {
        var externalInfo = new ExternalInfo();
        var dic = new Dictionary<string, string>
        {
            [SchrodingerContractConstants.InscriptionImageKey] = image
        };

        var info = new DeployInscriptionInfo
        {
            P = SchrodingerContractConstants.InscriptionType,
            Op = SchrodingerContractConstants.DeployOp,
            Tick = tick,
            Max = totalSupply == 0 ? SchrodingerContractConstants.Lim : totalSupply.ToString(),
            Lim = SchrodingerContractConstants.Lim
        };
        dic[SchrodingerContractConstants.InscriptionDeployKey] = info.ToString();

        externalInfo.Value.Add(dic);
        return externalInfo;
    }

    private void CreateInscription(string tick, int decimals, long totalSupply, ExternalInfo externalInfo,
        Address issuer)
    {
        var createTokenInput = new CreateInput
        {
            Symbol = GetInscriptionSymbol(tick),
            TokenName = GetInscriptionName(tick),
            TotalSupply = totalSupply,
            Decimals = decimals,
            Issuer = issuer ?? Context.Sender,
            IsBurnable = true,
            IssueChainId = Context.ChainId,
            ExternalInfo = externalInfo,
            Owner = Context.Self
        };
        State.TokenContract.Create.Send(createTokenInput);
    }

    private void CreateInscriptionCollection(string tick, int decimals, long totalSupply, ExternalInfo externalInfo,
        Address issuer, Address owner)
    {
        var createTokenInput = new CreateInput
        {
            Symbol = GetInscriptionCollectionSymbol(tick),
            TokenName = GetInscriptionCollectionName(tick),
            TotalSupply = totalSupply,
            Decimals = decimals,
            Issuer = issuer ?? Context.Sender,
            IsBurnable = true,
            IssueChainId = Context.ChainId,
            ExternalInfo = externalInfo,
            Owner = Context.Self
        };
        State.TokenContract.Create.Send(createTokenInput);
    }

    private void SetTokenContract()
    {
        if (State.TokenContract.Value == null)
        {
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        }
    }

    private string GetInscriptionSymbol(string tick)
    {
        return $"{tick}{SchrodingerContractConstants.Separator}{SchrodingerContractConstants.AncestorSymbolSuffix}";
    }

    private string GetInscriptionCollectionSymbol(string tick)
    {
        return $"{tick}{SchrodingerContractConstants.Separator}{SchrodingerContractConstants.CollectionSymbolSuffix}";
    }

    private string GetInscriptionName(string tick)
    {
        return $"{GetInscriptionSymbol(tick)}{SchrodingerContractConstants.AncestorNameSuffix}";
    }
    
    private string GetInscriptionCollectionName(string tick)
    {
        return $"{GetInscriptionCollectionSymbol(tick)}{SchrodingerContractConstants.AncestorNameSuffix}";
    }

    #endregion

    #region Attribute

    private AttributeLists SetAttributeList(string tick, long maxGen, AttributeLists toUpdateAttributeList,
        int attributesPerGen, out List<AttributeSet> toRemoveFixed, out List<AttributeSet> toRemoveRandom)
    {
        CheckAttributeList(toUpdateAttributeList);
        // distinct by trait type name
        var fixedAttributes = toUpdateAttributeList?.FixedAttributes?.DistinctBy(f => f.TraitType.Name).ToList();
        var randomAttributes = toUpdateAttributeList?.RandomAttributes?.DistinctBy(f => f.TraitType.Name).ToList();
        CheckForDuplicateTraitTypes(fixedAttributes, randomAttributes);
        var fixedAttributeSet = SetFixedAttributeSet(tick, fixedAttributes, out toRemoveFixed);
        var randomAttributeSet =
            SetRandomAttributeSet(tick, randomAttributes, maxGen, attributesPerGen, out toRemoveRandom);
        var result = new AttributeLists
        {
            FixedAttributes = { fixedAttributeSet },
            RandomAttributes = { randomAttributeSet }
        };
        CheckAttributeList(result);
        return result;
    }

    private void CheckAttributeList(AttributeLists attributeList)
    {
        Assert(
            attributeList != null &&
            attributeList.FixedAttributes != null && attributeList.FixedAttributes.Count > 0 &&
            attributeList.RandomAttributes != null && attributeList.RandomAttributes.Count > 0,
            "Invalid input attribute list.");
        CheckAttributeTraitTypeListCount(attributeList.FixedAttributes, attributeList.RandomAttributes);
    }

    private void CheckRandomAttributeList(IEnumerable<AttributeInfo> randomAttributes, long maxGen, int attributesPerGen)
    {
        Assert(randomAttributes?.Count() >= ((long)attributesPerGen).Mul(maxGen),
            "Invalid random attribute list count.");
    }

    private void CheckAttributeTraitTypeListCount(IEnumerable<AttributeSet> fixAttributes,
        IEnumerable<AttributeSet> randomAttributes)
    {
        var traitTypeMaxCount =
            State.Config.Value?.TraitTypeMaxCount ?? SchrodingerContractConstants.DefaultMaxAttributeTraitTypeCount;
        Assert(fixAttributes.Count().Add(randomAttributes.Count()) <= traitTypeMaxCount);
    }

    private void CheckForDuplicateTraitTypes(List<AttributeSet> fixedAttributes, List<AttributeSet> randomAttributes)
    {
        var intersection = fixedAttributes.Select(f => f.TraitType.Name)
            .Intersect(randomAttributes.Select(f => f.TraitType.Name));
        Assert(!intersection.Any(), "Trait type cannot be repeated.");
    }

    private List<AttributeSet> SetFixedAttributeSet(string tick, List<AttributeSet> sourceAttributeSets,
        out List<AttributeSet> toRemove)
    {
        toRemove = new List<AttributeSet>();
        var traitTypeMap = State.FixedTraitTypeMap[tick] ?? new AttributeInfos();
        traitTypeMap = SetAttributeSet(tick, traitTypeMap, sourceAttributeSets, out var result, out toRemove);
        State.FixedTraitTypeMap[tick] = traitTypeMap;
        return result;
    }

    private List<AttributeSet> SetRandomAttributeSet(string tick, List<AttributeSet> sourceAttributeSets, long maxGen,
        int attributesPerGen, out List<AttributeSet> toRemove)
    {
        toRemove = new List<AttributeSet>();
        var traitTypeMap = State.RandomTraitTypeMap[tick] ?? new AttributeInfos();
        traitTypeMap = SetAttributeSet(tick, traitTypeMap, sourceAttributeSets, out var result, out toRemove);
        State.RandomTraitTypeMap[tick] = traitTypeMap;
        CheckRandomAttributeList(traitTypeMap.Data, maxGen, attributesPerGen);
        return result;
    }

    private AttributeInfos UpdateTraitTypeAndSetValue(string tick, AttributeInfos traitTypeMap,
        AttributeSet toAddAttribute,
        out AttributeSet toRemove, out AttributeSet updateToAddAttribute)
    {
        updateToAddAttribute = toAddAttribute.Clone();
        toRemove = null;
        var traitType = toAddAttribute.TraitType;
        if (!CheckAttributeExist(traitType.Name, traitTypeMap))
        {
            // trait type not exist
            traitTypeMap.Data.Add(traitType);
            updateToAddAttribute.Values = SetTraitValues(tick, traitType.Name, toAddAttribute.Values);
        }
        else
        {
            // trait type exist
            if (CheckAndRemoveAttributeSet(tick, traitTypeMap, toAddAttribute, out toRemove, out traitTypeMap))
            {
                // trait value != null and reset trait values
                updateToAddAttribute.Values = SetTraitValues(tick, traitType.Name, toAddAttribute.Values);
            }
        }

        return traitTypeMap;
    }

    private AttributeInfos SetAttributeSet(string tick, AttributeInfos traitTypeMap,
        List<AttributeSet> sourceAttributeSets, out List<AttributeSet> result, out List<AttributeSet> toRemove)
    {
        result = new List<AttributeSet>();
        toRemove = new List<AttributeSet>();
        foreach (var toAddAttribute in sourceAttributeSets)
        {
            var traitType = toAddAttribute.TraitType;
            CheckAttributeInfo(traitType);
            traitTypeMap = UpdateTraitTypeAndSetValue(tick, traitTypeMap, toAddAttribute, out var toRemoveItem,
                out var updateToAddAttribute);
            result.Add(updateToAddAttribute);
            if (toRemoveItem != null)
            {
                toRemove.Add(toRemoveItem);
            }
        }

        return traitTypeMap;
    }

    private AttributeInfos SetTraitValues(string tick, string traitTypeName, AttributeInfos toUpdateTraitValues)
    {
        Assert(toUpdateTraitValues != null && toUpdateTraitValues.Data.Count > 0, "Invalid attribute trait values.");
        var traitValues = toUpdateTraitValues?.Data.DistinctBy(f => f.Name).ToList();
        CheckTraitValueCount(traitValues);
        var traitValueMap = State.TraitValueMap[tick][traitTypeName] ?? new AttributeInfos();
        traitValueMap.Data.Clear();
        foreach (var toUpdateTraitValue in traitValues)
        {
            CheckAttributeInfo(toUpdateTraitValue);
            traitValueMap.Data.Add(toUpdateTraitValue);
        }

        State.TraitValueMap[tick][traitTypeName] = traitValueMap;
        return toUpdateTraitValues;
    }

    private List<AttributeSet> GetAttributes(string tick, AttributeInfos attributeInfos)
    {
        return attributeInfos.Data.Select(traitType => new AttributeSet
        {
            TraitType = traitType,
            Values = new AttributeInfos { Data = { State.TraitValueMap[tick][traitType.Name]?.Data } }
        }).ToList();
    }

    #endregion

    #region Param check

    private void CheckDeployParams(DeployInput input)
    {
        CheckInitialized();
        Assert(!string.IsNullOrWhiteSpace(input.Tick) &&
               input.Decimals >= 0 && input.TotalSupply > 0 && input.LossRate > 0 && input.CommissionRate > 0,
            "Invalid input.");
        CheckDeployPermission(input.Tick);
        CheckGeneration(input.MaxGeneration);
        CheckAttributePerGen(input.AttributesPerGen, input.MaxGeneration);
        CheckImageSize(input.Image);
        CheckImageCount(input.ImageCount);
        CheckCrossGenerationConfig(input.CrossGenerationConfig,input.MaxGeneration);
    }

    private void CheckDeployPermission(string tick)
    {
        SetTokenContract();
        var issuer = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
        {
            Symbol = GetInscriptionCollectionSymbol(tick)
        }).Issuer;
        Assert(issuer == Context.Sender,"No permission to create.");
    }

    private void CheckGeneration(int maxGen)
    {
        var config = State.Config?.Value;
        var max = config?.MaxGen ?? SchrodingerContractConstants.DefaultMaxGen;
        Assert(maxGen >= SchrodingerContractConstants.DefaultMinGen && maxGen <= max, "Invalid max generation.");
    }

    private void CheckImageSize(string image)
    {
        var config = State.Config?.Value;
        var maxImageSize = config?.ImageMaxSize ?? SchrodingerContractConstants.DefaultImageMaxSize;
        Assert(!string.IsNullOrWhiteSpace(image) && Encoding.UTF8.GetByteCount(image) <= maxImageSize,
            "Invalid image data.");
    }

    private void CheckImageCount(int imageCount)
    {
        var config = State.Config?.Value;
        var maxImageCount = config?.ImageMaxCount ?? SchrodingerContractConstants.DefaultImageMaxCount;
        Assert(imageCount > 0 && imageCount <= maxImageCount, "Invalid image count.");
    }

    private void CheckCrossGenerationConfig(CrossGenerationConfig crossGenerationConfig, int maxGen)
    {
        Assert(
            crossGenerationConfig.Gen >= 0 && crossGenerationConfig.Gen <= maxGen &&
            crossGenerationConfig.CrossGenerationProbability >= 0 &&
            crossGenerationConfig.CrossGenerationProbability <=
            SchrodingerContractConstants.Denominator &&
            crossGenerationConfig.Weights.Count == crossGenerationConfig.Gen,
            "Invalid cross generation config.");
    }

    private void CheckAttributePerGen(int attributesPerGen, int maxGen)
    {
        var config = State.Config?.Value;
        var maxAttributePerGen = config?.MaxAttributesPerGen ?? SchrodingerContractConstants.DefaultMaxAttributePerGen;
        Assert(attributesPerGen > 0 && attributesPerGen <= maxAttributePerGen && attributesPerGen <= maxGen,
            "Invalid attributes per gen.");
    }

    private void CheckAttributeInfo(AttributeInfo attributeInfo)
    {
        var attributeMaxLength =
            State.Config?.Value?.AttributeMaxLength ?? SchrodingerContractConstants.DefaultAttributeMaxLength;
        Assert(
            !string.IsNullOrEmpty(attributeInfo.Name) && attributeInfo.Name.Length <= attributeMaxLength &&
            CheckAttributeWeight(attributeInfo.Weight), "Invalid trait value.");
    }


    private bool CheckAttributeWeight(long weight)
    {
        return weight > 0 && weight <= SchrodingerContractConstants.DefaultMaxAttributeWeight;
    }

    // if attribute exists return true.
    private bool CheckAttributeExist(string traitTypeName, AttributeInfos traitTypeMap)
    {
        var existingTraitType = traitTypeMap.Data.FirstOrDefault(f => f.Name == traitTypeName);
        return existingTraitType != null;
    }

    private void CheckTraitValueCount(List<AttributeInfo> traitValues)
    {
        var maxTraitValueCount = State.Config?.Value?.TraitValueMaxCount ??
                                 SchrodingerContractConstants.DefaultTraitValueMaxCount;
        Assert(traitValues?.Count > 0 && traitValues?.Count <= maxTraitValueCount,
            "Invalid attribute trait values count.");
    }

    // if don't need to set trait value return false.
    private bool CheckAndRemoveAttributeSet(string tick, AttributeInfos traitTypeMap, AttributeSet attribute,
        out AttributeSet toRemove, out AttributeInfos updateTraitTypeMap)
    {
        updateTraitTypeMap = traitTypeMap;
        toRemove = null;
        if (attribute.Values != null && attribute.Values.Data.Count != 0) return true;
        // remove trait type and trait values.
        Assert(traitTypeMap.Data.Remove(attribute.TraitType), "Remove failed.");
        State.TraitValueMap[tick].Remove(attribute.TraitType.Name);
        toRemove = attribute;
        updateTraitTypeMap = traitTypeMap;
        return false;
    }

    private InscriptionInfo CheckInscriptionExistAndPermission(string tick)
    {
        var inscription = State.InscriptionInfoMap[tick];
        Assert(inscription != null, "Inscription not found.");
        Assert(inscription.Admin == Context.Sender, "No permission.");
        return inscription;
    }

    #endregion
}