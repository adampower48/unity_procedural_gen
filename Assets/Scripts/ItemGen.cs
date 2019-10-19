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
    public WeaponModifier[] weaponModifiers;

    public int maxNumWeaponModifiers;


    // Start is called before the first frame update
    void Start()
    {
        for (var i = 0; i < 10; i++)
        {
            var weapon = GenerateRandomWeapon();
            Debug.Log("Weapon:" + weapon);
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    public Weapon GenerateRandomWeapon(int seed = -1)
    {
        if (seed != -1) Random.InitState(seed);

        var pre = damageModifiers[Random.Range(0, damageModifiers.Length)];
        var suf = damageModifiers[Random.Range(0, damageModifiers.Length)];
        var type = weaponTypes[Random.Range(0, weaponTypes.Length)];

        // Modifiers
        var numWeaponMods = Random.Range(0, Mathf.Min(maxNumWeaponModifiers, weaponModifiers.Length + 1));
        var weapMods = new WeaponModifier[numWeaponMods];
        for (var i = 0; i < numWeaponMods; i++)
        {
            // todo: dont pick duplicates, dont pick opposites (eg dull + sharp), might need to iteratively reduce mod pool
            weapMods[i] = weaponModifiers[Random.Range(0, weaponModifiers.Length)];
        }

        return new Weapon(pre, suf, type, weapMods);
    }
}

public class Weapon
{
    DamageModifier _prefix;
    DamageModifier _suffix;
    WeaponType _type;
    WeaponModifier[] _extraModifiers;

    public Weapon(DamageModifier prefix, DamageModifier suffix, WeaponType type, WeaponModifier[] extraModifiers)
    {
        _prefix = prefix;
        _suffix = suffix;
        _type = type;
        _extraModifiers = extraModifiers;
    }

    public override string ToString()
    {
        // Name
        var weapName = _prefix.prefixName + " " + _type.name + " " + _suffix.suffixName;

        // Weapon stats
        var stats = String.Format("Speed: {0:F2}\nDPS: {1:F2}", GetAttackSpeed(), GetDps());

        // Damage types
        var dtypeStr = "";
        foreach (var entry in GetDamageByType())
        {
            dtypeStr += entry.Key + ": " + entry.Value + "\n";
        }

        // Modifiers
        var mods = "Mods: ";
        foreach (var mod in _extraModifiers)
        {
            mods += mod.name + " ";
        }


        return weapName + "\n" + mods + "\n" + stats + "\n" + dtypeStr;
    }

    public float GetDamage()
    {
        // Damage per hit
        return _prefix.damage + GetBaseDamage() + _suffix.damage;
    }

    public float GetDps()
    {
        // Damage per second
        return GetDamage() * GetAttackSpeed();
    }

    public float GetBaseDamage()
    {
        float damage = _type.baseDamage;
        foreach (var mod in _extraModifiers)
        {
            damage *= mod.baseDamageMulti;
        }

        return damage;
    }

    public float GetAttackSpeed()
    {
        var speed = _type.attackSpeed;
        foreach (var mod in _extraModifiers)
        {
            speed *= mod.attackSpeedMulti;
        }

        return speed;
    }

    public Dictionary<string, float> GetDamageByType() // todo:
    {
        var damages = new Dictionary<string, float>();

        // Weapon damage
        var dtype = "Physcial";
        if (!damages.ContainsKey(dtype)) damages.Add(dtype, GetBaseDamage());
        else damages[dtype] += GetBaseDamage();

        // Pre/suffixes
        dtype = _prefix.damageType;
        if (!damages.ContainsKey(dtype)) damages.Add(dtype, _prefix.damage);
        else damages[dtype] += _prefix.damage;

        dtype = _suffix.damageType;
        if (!damages.ContainsKey(dtype)) damages.Add(dtype, _suffix.damage);
        else damages[dtype] += _suffix.damage;


        return damages;
    }
}