using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

// ItemType에 대응되는 IActiveItem 구현체를 찾아주는 컴포넌트.
//
// 다형성이 있는 객체의 직렬화 처리가 복잡한 관계로 아이템 정보를
// save/load할 때 아이템 전체가 아니라 ItemType만 저장하는 방식을 선택했음.
//
// 팩토리 패턴처럼 객체를 생성하지 않고 굳이 prefab을 사용하는 이유는
// IActiveItem의 자식 클래스를 monobehavior로 만들어야만
// 생성할 투사체의 prefab 같은 정보를 에디터에서 레퍼런스로 지정할 수 있기 때문.
//
// 이렇게 하지 않으면 Resources.Load같은 함수에 문자열 경로를 하드코딩해야하는 것으로 알고있음. 
public class ActiveItemDatabase : MonoBehaviour
{
    // 배열의 각 위치에 인덱스와 동일한 값을 갖는 ItemType에
    // 해당하는 아이템의 prefab을 넣어두고 사용하는 방식.
    // Ex) FireGrenade의 값은 0 => 배열의 0번 원소는 FireGrenade의 prefab.
    [SerializeField] private List<GameObject> itemPrefabs;

    public IActiveItem GetItemInstance(ItemType type)
    {
        // case 1) 아이템이 없는 경우
        if (type == ItemType.None)
        {
            return null;
        }

        // case 2) 뭔가 아이템이 있는 경우
        int itemIndex = (int)type;
        Assert.IsTrue(itemIndex < itemPrefabs.Count);

        return itemPrefabs[itemIndex].GetComponent<IActiveItem>();
    }

    public GameObject GetItemPrefab(ItemType type)
    {
        // case 1) 아이템이 없는 경우
        if (type == ItemType.None)
        {
            return null;
        }

        // case 2) 뭔가 아이템이 있는 경우
        int itemIndex = (int)type;
        Assert.IsTrue(itemIndex < itemPrefabs.Count);

        return itemPrefabs[itemIndex];
    }

    // TODO: 희귀도에 따라 하나 랜덤하게 골라주는 함수 만들기
    

    public ItemType ChooseRandomItemType()
    {
        ItemRarity rarity = ChooseRarity();
        return ChooseItemTypeByRarity(rarity);
    }

    private ItemRarity ChooseRarity()
    {
        // TODO: 행운 스탯에 비례해 전설 확률 올리고 그만큼 흔함 확률 내리기
        const float COMMON_ITEM_PROBABILITY = 0.70f;
        const float RARE_ITEM_PROBABILITY = 0.25f;
        const float LEGENDARY_ITEM_PROBABILITY = 0.05f;
        Assert.IsTrue(Mathf.Approximately(COMMON_ITEM_PROBABILITY + RARE_ITEM_PROBABILITY + LEGENDARY_ITEM_PROBABILITY, 1f));

        var value = UnityEngine.Random.Range(0f, 1f);
        if (value < COMMON_ITEM_PROBABILITY)
        {
            return ItemRarity.Common;
        }
        else if (value < COMMON_ITEM_PROBABILITY + RARE_ITEM_PROBABILITY)
        {
            return ItemRarity.Rare;
        }
        else
        {
            return ItemRarity.Legendary;
        }
    }

    private ItemType ChooseItemTypeByRarity(ItemRarity rarity)
    {
        // None에 해당하는 0번을 제외한 나머지 아이템을 순회하며
        // rarity가 일치하는 ItemType 목록을 구축
        List<ItemType> itemTypes = new();
        for (int i = 1; i < itemPrefabs.Count; ++i)
        {
            var item = itemPrefabs[i].GetComponent<IActiveItem>();
            if (item.GetRarity() == rarity)
            {
                itemTypes.Add(item.GetType());
            }
        }

        // 목록 중에서 하나를 랜덤하게 선택
        return itemTypes[Random.Range(0, itemTypes.Count)];
    }
}
