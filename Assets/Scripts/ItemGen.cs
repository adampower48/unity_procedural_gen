using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public struct WeaponType
{
    public string name;
    public float attackSpeed;
    public int baseDamage;


}

[Serializable]
public struct DamageModifier
{
    public string prefixName;
    public string suffixName;
    public string damageType;
    public int damage;

}

[Serializable]
public struct WeaponModifier
{
    public string name;
    public float attackSpeedMulti;
    public float baseDamageMulti;
}



public class ItemGen : MonoBehaviour
{
    public DamageModifier[] damageModifiers;
    public WeaponType[] weaponTypes;
    public WeaponModifier[] weaponModifers;

    public int maxNumWeaponModifiers;


    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < 10; i++)
        {
            Weapon weapon = GenerateRandomWeapon();
            Debug.Log("Weapon:" + weapon);

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Weapon GenerateRandomWeapon(int seed=-1)
    {
        if (seed != -1) Random.InitState(seed);

        var pre = damageModifiers[Random.Range(0, damageModifiers.Length)];
        var suf = damageModifiers[Random.Range(0, damageModifiers.Length)];
        var type = weaponTypes[Random.Range(0, weaponTypes.Length)];

        // Modifiers
        int numWeaponMods = Random.Range(0, Mathf.Min(maxNumWeaponModifiers, weaponModifers.Length + 1));
        WeaponModifier[] weapMods = new WeaponModifier[numWeaponMods];
        for (int i = 0; i < numWeaponMods; i++)
        {
            // todo: dont pick duplicates, dont pick opposites (eg dull + sharp), might need to iteratively reduce mod pool
            weapMods[i] = weaponModifers[Random.Range(0, weaponModifers.Length)];
        }

        return new Weapon(pre, suf, type, weapMods);
    }

    

}

public class Weapon
{
    DamageModifier prefix;
    DamageModifier suffix;
    WeaponType type;
    WeaponModifier[] extraModifiers;

    public Weapon(DamageModifier _prefix, DamageModifier _suffix, WeaponType _type, WeaponModifier[] _extraModifiers)
    {
        prefix = _prefix;
        suffix = _suffix;
        type = _type;
        extraModifiers = _extraModifiers;
    }

    public override string ToString()
    {
        // Name
        string weapName = prefix.prefixName + " " + type.name + " " + suffix.suffixName;

        // Weapon stats
        string stats = String.Format("Speed: {0:F2}\nDPS: {1:F2}", GetAttackSpeed(), GetDPS());

        // Damage types
        string dtypeStr = "";
        foreach(var entry in GetDamageByType())
        {
            dtypeStr += entry.Key + ": " + entry.Value + "\n";
        }

        // Modifiers
        string mods = "Mods: ";
        foreach (var mod in extraModifiers)
        {
            mods += mod.name + " ";
        }


        return weapName + "\n" + mods + "\n" + stats + "\n" + dtypeStr;
    }

    public float GetDamage()
    {
        // Damage per hit
        return prefix.damage + GetBaseDamage() + suffix.damage;
    }

    public float GetDPS()
    {
        // Damage per second
        return GetDamage() * GetAttackSpeed();
    }

    public float GetBaseDamage()
    {
        float damage = type.baseDamage;
        foreach (var mod in extraModifiers)
        {
            damage *= mod.baseDamageMulti;
        }

        return damage;
    }

    public float GetAttackSpeed()
    {
        float speed = type.attackSpeed;
        foreach (var mod in extraModifiers)
        {
            speed *= mod.attackSpeedMulti;
        }

        return speed;
    }

    public Dictionary<string, float> GetDamageByType() // todo:
    {
        Dictionary<string, float> damages = new Dictionary<string, float>();

        // Weapon damage
        string dtype = "Physcial";
        if (!damages.ContainsKey(dtype)) damages.Add(dtype, GetBaseDamage());
        else damages[dtype] += GetBaseDamage();

        // Pre/suffixes
        dtype = prefix.damageType;
        if (!damages.ContainsKey(dtype)) damages.Add(dtype, prefix.damage);
        else damages[dtype] += prefix.damage;

        dtype = suffix.damageType;
        if (!damages.ContainsKey(dtype)) damages.Add(dtype, suffix.damage);
        else damages[dtype] += suffix.damage;



        return damages;
    }
}
