using System.Collections.Generic;
using System.Linq;
using System.Text;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Collections;

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
        return !input.IsNullOrEmpty();
    }

    private bool IsSymbolValid(string input)
    {
        return IsStringValid(input) && input.Split(SchrodingerContractConstants.Separator).Length == 2;
    }

    private string GetTickFromSymbol(string symbol)
    {
        return symbol.Split(SchrodingerContractConstants.Separator)[0].ToUpper();
    }

    private InscriptionInfo CheckInscriptionExistAndPermission(string tick)
    {
        var inscription = State.InscriptionInfoMap[tick];
        Assert(inscription != null, "Inscription not found.");
        Assert(inscription.Admin == Context.Sender, "No permission.");
        return inscription;
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
            Max = totalSupply.ToString(),
            Lim = totalSupply.ToString()
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
            TokenName = tick,
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

    #endregion

    #region Attribute

    /// <param name="tick"></param>
    /// <param name="maxGen"></param>
    /// <param name="sourceAttributeList">to add attribute list, contains fixed and random</param>
    /// <param name="attributesPerGen"></param>
    /// <returns></returns>
    private AttributeLists SetAttributeList(string tick, long maxGen, AttributeLists sourceAttributeList,
        int attributesPerGen)
    {
        var fixedAttributes = sourceAttributeList?.FixedAttributes;
        var randomAttributes = sourceAttributeList?.RandomAttributes;
        CheckAttributeList(fixedAttributes, randomAttributes); 
        SetFixedAttributeSets(tick, fixedAttributes);
        SetRandomAttributeSet(tick, randomAttributes, maxGen, attributesPerGen);
        var result = new AttributeLists
        {
            FixedAttributes = { fixedAttributes },
            RandomAttributes = { randomAttributes }
        };
        return result;
    }

    /// <summary>
    /// Set fixed attribute sets.
    /// </summary>
    /// <param name="tick"></param>
    /// <param name="sourceAttributeSets"> to set attribute sets</param>
    /// <param name="toRemove">out to removed attribute sets.</param>
    /// <returns></returns>
    private void SetFixedAttributeSets(string tick, RepeatedField<AttributeSet> sourceAttributeSets)
    {
        var traitTypeMap = State.FixedTraitTypeMap[tick] ?? new AttributeInfos();
        SetAttributeSet(tick, traitTypeMap, sourceAttributeSets);
        State.FixedTraitTypeMap[tick] = traitTypeMap;
    }

    /// <summary>
    /// Set random attribute sets.
    /// After changed, check random attribute list count according to maxGen and attributesPerGen.
    /// </summary>
    /// <param name="tick"></param>
    /// <param name="sourceAttributeSets"> to set attribute sets</param>
    /// <param name="maxGen"></param>
    /// <param name="attributesPerGen"></param>
    /// <param name="toRemove">out to removed attribute sets.</param>
    /// <returns></returns>
    private void SetRandomAttributeSet(string tick, RepeatedField<AttributeSet> sourceAttributeSets,
        long maxGen,
        int attributesPerGen)
    {
        var traitTypeMap = State.RandomTraitTypeMap[tick] ?? new AttributeInfos();
        SetAttributeSet(tick, traitTypeMap, sourceAttributeSets, true);
        State.RandomTraitTypeMap[tick] = traitTypeMap;
        CheckRandomAttributeList(traitTypeMap.Data, maxGen, attributesPerGen);
    }

    /// <param name="tick"></param>
    /// <param name="traitTypeMap">trait type list from state</param>
    /// <param name="sourceAttributeSet">input attributeSet.</param>
    /// <param name="toRemove"> The trait type exists, the list of source trait values is empty, remove the trait type and trait values.</param>
    /// <param name="updateAttributeSet">attributeSet after update</param>
    /// <returns></returns>
    private AttributeInfos UpdateTraitTypeAndSetValue(string tick, AttributeInfos traitTypeMap,
        AttributeSet sourceAttributeSet, out AttributeSet updateAttributeSet)
    {
        updateAttributeSet = sourceAttributeSet.Clone();
        var traitType = sourceAttributeSet.TraitType;
        if (!CheckTraitTypeExist(tick, traitType.Name))
        {
            // trait type not exist
            traitTypeMap.Data.Add(traitType);
            updateAttributeSet.Values = SetTraitValues(tick, traitType.Name, sourceAttributeSet.Values);
        }
        else
        {
            // trait type exist
            if (CheckAndRemoveAttributeSet(tick, traitTypeMap, sourceAttributeSet, out traitTypeMap))
            {
                // trait value != null and reset trait values
                updateAttributeSet.Values = SetTraitValues(tick, traitType.Name, sourceAttributeSet.Values);
            }
        }

        return traitTypeMap;
    }

    /// <param name="tick"></param>
    /// <param name="traitTypeMap">trait type list from state</param>
    /// <param name="sourceAttributeSet">input attributeSet.</param>
    /// <param name="toRemove"> The trait type exists, the list of source trait values is empty, remove the trait type and trait values.</param>
    /// <param name="updateAttributeSet">attributeSet after update</param>
    /// <returns></returns>
    private AttributeInfos SetTraitValue(string tick, AttributeInfos traitTypeMap,
        AttributeSet sourceAttributeSet, out AttributeSet updateAttributeSet)
    {
        updateAttributeSet = sourceAttributeSet.Clone();
        var traitType = sourceAttributeSet.TraitType;
        if (!CheckTraitTypeExist(tick, traitType.Name))
        {
            // trait type not exist
            traitTypeMap.Data.Add(traitType);
            updateAttributeSet.Values = SetTraitValues(tick, traitType.Name, sourceAttributeSet.Values);
        }
        else
        {
            // trait type exist
            if (CheckAndRemoveAttributeSet(tick, traitTypeMap, sourceAttributeSet, out traitTypeMap))
            {
                // trait value != null and reset trait values
                updateAttributeSet.Values = SetTraitValues(tick, traitType.Name, sourceAttributeSet.Values);
            }
        }

        return traitTypeMap;
    }

    // 
    /// <summary>
    /// return trait type list,out update attribute sets(trait type and values),out to remove attribute sets.
    /// </summary>
    /// <param name="tick"></param>
    /// <param name="traitTypeMap">trait type list from state</param>
    /// <param name="sourceAttributeSets">input attributeSets</param>
    /// <param name="updateAttributeSets">out update attribute set, remove duplication from trait value list</param>
    /// <param name="toRemove">out remove list</param>
    /// <param name="isRandom">if is random trait type</param>
    /// <returns></returns>
    private void SetAttributeSet(string tick, AttributeInfos traitTypeMap,
        RepeatedField<AttributeSet> sourceAttributeSets, bool isRandom = false)
    {
        foreach (var sourceAttributeSet in sourceAttributeSets)
        {
            var traitType = sourceAttributeSet.TraitType;
            if (isRandom)
            {
                CheckTraitTypeRepeated(tick, traitType.Name);
            }
            CheckAttributeInfo(traitType);
            SetTraitValues(tick, traitType.Name, sourceAttributeSet.Values);
            traitTypeMap.Data.Add(traitType);
        }
    }

    /// <param name="tick"></param>
    /// <param name="traitTypeName"></param>
    /// <param name="sourceTraitValues"> input trait values</param>
    /// <returns>after changed,example remove duplicates</returns>
    private AttributeInfos SetTraitValues(string tick, string traitTypeName, AttributeInfos sourceTraitValues)
    {
        Assert(sourceTraitValues != null && sourceTraitValues.Data.Count > 0, "Invalid attribute trait values.");
        var uniqueSet = new HashSet<string>();
        var traitValueMap = State.TraitValueMap[tick][traitTypeName] ?? new AttributeInfos();
        var data = traitValueMap.Data;
        var traitValuesCount = 0;
        data.Clear();
        foreach (var sourceTraitValue in sourceTraitValues.Data)
        {
            if (!uniqueSet.Add(sourceTraitValue.Name)) continue;
            CheckAttributeInfo(sourceTraitValue);
            data.Add(sourceTraitValue);
            traitValuesCount++;
        }
        CheckTraitValueCount(traitValuesCount);
        State.TraitValueMap[tick][traitTypeName] = traitValueMap;
        return traitValueMap;
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

    #region Attribute param check

    private void CheckAttributeListDuplicate(RepeatedField<AttributeSet> attributeSets)
    {
        var unique = new HashSet<string>();
        foreach (var set in attributeSets)
        {
            Assert(unique.Add(set.TraitType.Name), "Duplicate attribute type.");
        }
    }

    private void CheckAttributeList(RepeatedField<AttributeSet> fixedAttributeSets,
        RepeatedField<AttributeSet> randomAttributeSets)
    {
        Assert(fixedAttributeSets != null && randomAttributeSets != null, "Invalid input attribute list.");
        var fixedCount = fixedAttributeSets.Count;
        var randomCount = randomAttributeSets.Count;
        var traitTypeMaxCount = State.Config.Value?.TraitTypeMaxCount ??
                                SchrodingerContractConstants.DefaultMaxAttributeTraitTypeCount;
        var fixedTraitTypeMaxCount = State.Config.Value?.FixedTraitTypeMaxCount ??
                                     SchrodingerContractConstants.DefaultFixedTraitTypeMaxCount;
        Assert(fixedCount > 0 && fixedCount <= fixedTraitTypeMaxCount, "Invalid input fixed attribute list count.");
        Assert(randomCount > 0, "Invalid input random attribute list count.");
        Assert(fixedCount.Add(randomCount) <= traitTypeMaxCount, "Fixed and random list exceed.");
        CheckAttributeListDuplicate(fixedAttributeSets);
        CheckAttributeListDuplicate(randomAttributeSets);
    }

    private void CheckRandomAttributeList(RepeatedField<AttributeInfo> randomAttributes, long maxGen,
        int attributesPerGen)
    {
        Assert(randomAttributes?.Count >= ((long)attributesPerGen).Mul(maxGen),
            "Invalid random attribute list count.");
    }

    private void CheckTraitValueCount(long traitValueCounts)
    {
        var maxTraitValueCount = State.Config?.Value?.TraitValueMaxCount ??
                                 SchrodingerContractConstants.DefaultTraitValueMaxCount;
        Assert(traitValueCounts > 0 && traitValueCounts <= maxTraitValueCount,
            $"Invalid attribute trait values count.Max count is {maxTraitValueCount}");
    }

    /// <summary>
    /// if don't need to set trait value return false.
    /// </summary>
    /// <param name="tick"></param>
    /// <param name="traitTypeMap"> trait type list from state</param>
    /// <param name="attribute">input attribute set</param>
    /// <param name="toRemove">to remove attributeSet, the list of source trait values is empty, remove the trait type and trait values.</param>
    /// <param name="updateTraitTypeMap">trait type list after changed</param>
    /// <returns></returns>
    private bool CheckAndRemoveAttributeSet(string tick, AttributeInfos traitTypeMap, AttributeSet attribute,
        out AttributeInfos updateTraitTypeMap)
    {
        updateTraitTypeMap = traitTypeMap;
        if (attribute.Values != null && attribute.Values.Data.Count != 0) return true;
        // remove trait type and trait values.
        Assert(traitTypeMap.Data.Remove(attribute.TraitType), "Remove failed.");
        State.TraitValueMap[tick].Remove(attribute.TraitType.Name);
        updateTraitTypeMap = traitTypeMap;
        return false;
    }

    private void CheckAttributePerGen(int attributesPerGen, int maxGen)
    {
        var config = State.Config?.Value;
        var maxAttributePerGen = config?.MaxAttributesPerGen ?? SchrodingerContractConstants.DefaultMaxAttributePerGen;
        Assert(attributesPerGen > 0 && attributesPerGen <= maxGen,
            "Invalid attributes per gen.");
        Assert(attributesPerGen <= maxAttributePerGen, "Attributes per generation need smaller than max.");
    }

    private void CheckAttributeInfo(AttributeInfo attributeInfo)
    {
        var attributeMaxLength =
            State.Config?.Value?.AttributeMaxLength ?? SchrodingerContractConstants.DefaultAttributeMaxLength;
        Assert(IsStringValid(attributeInfo.Name), "Invalid trait type name.");
        Assert(attributeInfo.Name.Length <= attributeMaxLength, "Invalid trait type name length.");
        CheckAttributeWeight(attributeInfo.Weight);
    }


    private void CheckAttributeWeight(long weight)
    {
        Assert(weight >= 0 && weight < SchrodingerContractConstants.DefaultMaxAttributeWeight, "Invalid weight.");
    }

    // if attribute exists return true.
    private bool CheckTraitTypeExist(string tick, string traitTypeName)
    {
        var value = State.TraitValueMap[tick][traitTypeName];
        return value != null;
    }

    private void CheckTraitTypeRepeated(string tick, string traitTypeName)
    {
        var fixedTraitTypeMap = State.FixedTraitTypeMap[tick].Data;
        foreach (var traitType in fixedTraitTypeMap)
        {
            Assert(traitType.Name != traitTypeName, "Trait type repeated.");
        }
    }

    #endregion

    #region Param check

    private void CheckDeployParams(DeployInput input)
    {
        CheckInitialized();
        Assert(IsStringValid(input.Tick), "Invalid input tick.");
        Assert(input.Decimals >= 0, "Invalid input decimals.");
        Assert(input.TotalSupply > 0, "Invalid input total supply.");

        CheckRate(input.LossRate, input.CommissionRate);
        CheckDeployPermission(input.Tick);
        CheckGeneration(input.MaxGeneration);
        CheckAttributePerGen(input.AttributesPerGen, input.MaxGeneration);
        CheckImageSize(input.Image);
        CheckImageCount(input.ImageCount);
        CheckCrossGenerationConfig(input.CrossGenerationConfig, input.MaxGeneration);
    }

    private void CheckRate(long lossRate, long commissionRate)
    {
        Assert(lossRate >= 0 && lossRate <= SchrodingerContractConstants.Denominator, "Invalid loss rate.");
        Assert(commissionRate >= 0 && commissionRate <= SchrodingerContractConstants.Denominator,
            "Invalid commission rate.");
    }

    private void CheckDeployPermission(string tick)
    {
        SetTokenContract();
        var issuer = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
        {
            Symbol = GetInscriptionCollectionSymbol(tick)
        }).Issuer;
        Assert(issuer == Context.Sender, "No permission to create.");
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
        Assert(IsStringValid(image) && Encoding.UTF8.GetByteCount(image) <= maxImageSize,
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
        Assert(crossGenerationConfig.Gen >= 0 && crossGenerationConfig.Gen <= maxGen,
            "Invalid cross generation config gen.");
        Assert(crossGenerationConfig.CrossGenerationProbability >= 0 &&
               crossGenerationConfig.CrossGenerationProbability <= SchrodingerContractConstants.Denominator,
            "Invalid cross generation probability.");
        Assert(crossGenerationConfig.Weights.Count == crossGenerationConfig.Gen, "Invalid cross generation weights.");
    }

    #endregion
}