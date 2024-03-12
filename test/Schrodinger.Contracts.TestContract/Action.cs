using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Schrodinger.Contracts.Contract;

namespace Schrodinger.Contracts.TestContract;

public class TestContract : TestContractContainer.TestContractBase
{
    public override Empty SetAttributes(SetAttributesInput input)
    {
        var attributeList = SetAttributeList(input.Tick, 3, input.Attributes,
            1);
        return new Empty();
    }

    #region Attribute

    /// <param name="tick"></param>
    /// <param name="maxGen"></param>
    /// <param name="sourceAttributeList">to add attribute list, contains fixed and random</param>
    /// <param name="attributesPerGen"></param>
    /// <param name="toRemoveFixed">out to removed fixed attributeSets</param>
    /// <param name="toRemoveRandom">out to removed random attributeSets</param>
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
        AttributeSet sourceAttributeSet, out AttributeSet toRemove, out AttributeSet updateAttributeSet)
    {
        updateAttributeSet = sourceAttributeSet.Clone();
        toRemove = null;
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
        var traitTypeMaxCount = TestContractConstants.DefaultMaxAttributeTraitTypeCount;
        var fixedTraitTypeMaxCount = TestContractConstants.DefaultFixedTraitTypeMaxCount;
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

    /// <summary>
    /// Attribute list cannot be duplicated.
    /// Fixed and random trait type cannot be repeated.
    /// </summary>
    /// <param name="fixedAttributes"></param>
    /// <param name="randomAttributes"></param>
    // private void CheckForDuplicateTraitTypes(List<AttributeSet> fixedAttributes, List<AttributeSet> randomAttributes)
    // {
    //     CheckAttributeListDuplicate(fixedAttributes);
    //     CheckAttributeListDuplicate(randomAttributes);
    // }
    private void CheckTraitValueCount(long traitValueCounts)
    {
        var maxTraitValueCount =
            TestContractConstants.DefaultTraitValueMaxCount;
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

    private void CheckAttributePerGen(int attributesPerGen, int maxGen)
    {
        var maxAttributePerGen = TestContractConstants.DefaultMaxAttributePerGen;
        Assert(attributesPerGen > 0 && attributesPerGen <= maxGen,
            "Invalid attributes per gen.");
        Assert(attributesPerGen <= maxAttributePerGen, "Attributes per generation need smaller than max.");
    }

    private void CheckAttributeInfo(AttributeInfo attributeInfo)
    {
        var attributeMaxLength = TestContractConstants.DefaultAttributeMaxLength;
        Assert(IsStringValid(attributeInfo.Name), "Invalid trait type name.");
        Assert(attributeInfo.Name.Length <= attributeMaxLength, "Invalid trait type name length.");
        CheckAttributeWeight(attributeInfo.Weight);
    }

    private bool IsStringValid(string input)
    {
        return !string.IsNullOrWhiteSpace(input);
    }

    private void CheckAttributeWeight(long weight)
    {
        Assert(weight > 0 && weight <= TestContractConstants.DefaultMaxAttributeWeight, "Invalid weight.");
    }

    // if attribute exists return true.
    private bool CheckTraitTypeExist(string tick, string traitTypeName)
    {
        var value = State.TraitValueMap[tick][traitTypeName];
        return value != null;
    }

    private void CheckTraitTypeRepeated(string tick, string traitTypeName)
    {
        foreach (var traitType in State.FixedTraitTypeMap[tick].Data)
        {
            Assert(traitType.Name != traitTypeName, "Trait type repeated.");
        }
    }

    #endregion
}