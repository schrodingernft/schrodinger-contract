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
    /// <param name="toRemoveFixed">out to removed fixed attributeSets</param>
    /// <param name="toRemoveRandom">out to removed random attributeSets</param>
    /// <returns></returns>
    private AttributeLists SetAttributeList(string tick, long maxGen, AttributeLists sourceAttributeList,
        int attributesPerGen, out List<AttributeSet> toRemoveFixed, out List<AttributeSet> toRemoveRandom)
    {
        CheckAttributeList(sourceAttributeList);
        // distinct by trait type name
        var fixedAttributes = sourceAttributeList?.FixedAttributes.ToList();
        var randomAttributes = sourceAttributeList?.RandomAttributes.ToList();
        CheckForDuplicateTraitTypes(fixedAttributes, randomAttributes);
        var fixedAttributeSet = SetFixedAttributeSets(tick, fixedAttributes, out toRemoveFixed);
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

    /// <summary>
    /// Set fixed attribute sets.
    /// </summary>
    /// <param name="tick"></param>
    /// <param name="sourceAttributeSets"> to set attribute sets</param>
    /// <param name="toRemove">out to removed attribute sets.</param>
    /// <returns></returns>
    private List<AttributeSet> SetFixedAttributeSets(string tick, List<AttributeSet> sourceAttributeSets,
        out List<AttributeSet> toRemove)
    {
        toRemove = new List<AttributeSet>();
        var traitTypeMap = State.FixedTraitTypeMap[tick] ?? new AttributeInfos();
        traitTypeMap = SetAttributeSet(tick, traitTypeMap, sourceAttributeSets, out var updateFixedAttributeSets,
            out toRemove);
        State.FixedTraitTypeMap[tick] = traitTypeMap;
        return updateFixedAttributeSets;
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
    private List<AttributeSet> SetRandomAttributeSet(string tick, List<AttributeSet> sourceAttributeSets, long maxGen,
        int attributesPerGen, out List<AttributeSet> toRemove)
    {
        toRemove = new List<AttributeSet>();
        var traitTypeMap = State.RandomTraitTypeMap[tick] ?? new AttributeInfos();
        traitTypeMap = SetAttributeSet(tick, traitTypeMap, sourceAttributeSets, out var updateRandomAttributeSets,
            out toRemove);
        State.RandomTraitTypeMap[tick] = traitTypeMap;
        CheckRandomAttributeList(traitTypeMap.Data, maxGen, attributesPerGen);
        return updateRandomAttributeSets;
    }

    /// <param name="tick"></param>
    /// <param name="traitTypeMap">trait type list from state</param>
    /// <param name="sourceAttributeSet">input attributeSet.</param>
    /// <param name="toRemove"> The trait type exists, the list of source trait values is empty, remove the trait type and trait values.</param>
    /// <param name="updateAttributeSet">attributeSet after update</param>
    /// <returns></returns>
    private AttributeInfos UpdateTraitTypeAndSetValue(string tick, AttributeInfos traitTypeMap,
        AttributeSet sourceAttributeSet, out AttributeSet toRemove, out AttributeSet updateAttributeSet)
    {
        updateAttributeSet = sourceAttributeSet.Clone();
        toRemove = null;
        var traitType = sourceAttributeSet.TraitType;
        if (!CheckTraitTypeExist(traitType.Name, traitTypeMap))
        {
            // trait type not exist
            traitTypeMap.Data.Add(traitType);
            updateAttributeSet.Values = SetTraitValues(tick, traitType.Name, sourceAttributeSet.Values);
        }
        else
        {
            // trait type exist
            if (CheckAndRemoveAttributeSet(tick, traitTypeMap, sourceAttributeSet, out toRemove, out traitTypeMap))
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
    /// <returns></returns>
    private AttributeInfos SetAttributeSet(string tick, AttributeInfos traitTypeMap,
        List<AttributeSet> sourceAttributeSets, out List<AttributeSet> updateAttributeSets,
        out List<AttributeSet> toRemove)
    {
        updateAttributeSets = new List<AttributeSet>();
        toRemove = new List<AttributeSet>();
        foreach (var sourceAttributeSet in sourceAttributeSets)
        {
            var traitType = sourceAttributeSet.TraitType;
            CheckAttributeInfo(traitType);
            traitTypeMap = UpdateTraitTypeAndSetValue(tick, traitTypeMap, sourceAttributeSet, out var toRemoveItem,
                out var updateAttributeSet);
            updateAttributeSets.Add(updateAttributeSet);
            if (toRemoveItem != null)
            {
                toRemove.Add(toRemoveItem);
            }
        }

        return traitTypeMap;
    }

    /// <param name="tick"></param>
    /// <param name="traitTypeName"></param>
    /// <param name="sourceTraitValues"> input trait values</param>
    /// <returns>after changed,example remove duplicates</returns>
    private AttributeInfos SetTraitValues(string tick, string traitTypeName, AttributeInfos sourceTraitValues)
    {
        Assert(sourceTraitValues != null && sourceTraitValues.Data.Count > 0, "Invalid attribute trait values.");
        var traitValues = sourceTraitValues?.Data.DistinctBy(f => f.Name).ToList();
        CheckTraitValueCount(traitValues);
        var traitValueMap = State.TraitValueMap[tick][traitTypeName] ?? new AttributeInfos();
        traitValueMap.Data.Clear();
        foreach (var sourceTraitValue in traitValues)
        {
            CheckAttributeInfo(sourceTraitValue);
            traitValueMap.Data.Add(sourceTraitValue);
        }

        State.TraitValueMap[tick][traitTypeName] = traitValueMap;
        return new AttributeInfos
        {
            Data = { traitValues }
        };
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

    private void CheckAttributeListDuplicate(List<AttributeSet> attributeSets)
    {
        var hasDuplicates = attributeSets.GroupBy(x => x.TraitType.Name).Any(g => g.Count() > 1);
        Assert(!hasDuplicates, "Duplicate attribute type.");
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

    private void CheckRandomAttributeList(IEnumerable<AttributeInfo> randomAttributes, long maxGen,
        int attributesPerGen)
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

    /// <summary>
    /// Attribute list cannot be duplicated.
    /// Fixed and random trait type cannot be repeated.
    /// </summary>
    /// <param name="fixedAttributes"></param>
    /// <param name="randomAttributes"></param>
    private void CheckForDuplicateTraitTypes(List<AttributeSet> fixedAttributes, List<AttributeSet> randomAttributes)
    {
        CheckAttributeListDuplicate(fixedAttributes);
        CheckAttributeListDuplicate(randomAttributes);
        var intersection = fixedAttributes.Select(f => f.TraitType.Name)
            .Intersect(randomAttributes.Select(f => f.TraitType.Name));
        Assert(!intersection.Any(), "Fixed and random trait type cannot be repeated.");
    }

    private void CheckTraitValueCount(List<AttributeInfo> traitValues)
    {
        var maxTraitValueCount = State.Config?.Value?.TraitValueMaxCount ??
                                 SchrodingerContractConstants.DefaultTraitValueMaxCount;
        Assert(traitValues?.Count > 0 && traitValues?.Count <= maxTraitValueCount,
            "Invalid attribute trait values count.");
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
        Assert(lossRate > 0 && lossRate <= SchrodingerContractConstants.Denominator, "Invalid loss rate.");
        Assert(commissionRate > 0 && commissionRate <= SchrodingerContractConstants.Denominator,
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
        Assert(weight > 0 && weight <= SchrodingerContractConstants.DefaultMaxAttributeWeight, "Invalid weight.");
    }

    // if attribute exists return true.
    private bool CheckTraitTypeExist(string traitTypeName, AttributeInfos traitTypeMap)
    {
        var existingTraitType = traitTypeMap.Data.FirstOrDefault(f => f.Name == traitTypeName);
        return existingTraitType != null;
    }

    #endregion
}