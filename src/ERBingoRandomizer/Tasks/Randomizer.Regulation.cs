using Project.Params;
using Project.Settings;
using Project.Utility;
using FSParam;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using static FSParam.Param;
using static SoulsFormats.PARAM;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO.MemoryMappedFiles;
using System.Windows.Controls.Primitives;

namespace Project.Tasks;

public partial class Randomizer
{
    public SeedInfo SeedInfo { get; private set; }
    private readonly string _seed;
    private readonly Random _random;
    private Param _itemLotParam_map;
    private Param _itemLotParam_enemy;
    private Param _shopLineupParam;
    private Param _atkParam_Pc;
    private Param _equipMtrlSetParam;
    // Dictionaries
    private Dictionary<int, EquipParamWeapon> _weaponDictionary;
    private List<List<int>> WeaponShopLists; // UPDATE
    private List<int> merchantWeaponList; // UPDATE
    private Dictionary<int, EquipParamWeapon> _customWeaponDictionary;
    private Dictionary<int, string> _weaponNameDictionary;
    private Dictionary<int, EquipParamGoods> _goodsDictionary;
    private Dictionary<int, Magic> _magicDictionary;
    private Dictionary<ushort, List<Param.Row>> _weaponTypeDictionary;
    private Dictionary<byte, List<Param.Row>> _armorTypeDictionary;
    private Dictionary<byte, List<Param.Row>> _magicTypeDictionary;
    private Dictionary<int, WorldMapPieceParam> _worldMapPieceParamDictionary;
    public Task RandomizeRegulation()
    {
        _randomizerLog = new List<string>();
        randomizeStartingClassParams();
        _cancellationToken.ThrowIfCancellationRequested();
        randomizeWeaponLocations();
        _cancellationToken.ThrowIfCancellationRequested();
        randomizeShopLineupParam();
        _cancellationToken.ThrowIfCancellationRequested();
        randomizeShopLineupParamMagic();
        _cancellationToken.ThrowIfCancellationRequested();
        randomizeShopArmorParam();
        _cancellationToken.ThrowIfCancellationRequested();
        randomizePerfumeBottleLocations();
        patchAtkParam();
        patchSmithingStones();
        _cancellationToken.ThrowIfCancellationRequested();
        allocatedIDs = new HashSet<int>() { 2510000, };
        addPureBloodToLeyndellReplacingRuneArc();
        worldMap();
        writeFiles();
        writeLog();
        SeedInfo = new SeedInfo(_seed, Util.GetShaRegulation256Hash());
        string seedJson = JsonSerializer.Serialize(SeedInfo);
        File.WriteAllText(Config.LastSeedPath, seedJson);
        return Task.CompletedTask;
    }

    private void duplicate()
    {
        IEnumerable<Param.Row> eleonara = _itemLotParam_map.Rows.Where(id => id.ID == 111600);
        eleonara = eleonara.ToList();
        Debug.WriteLine(eleonara.First().ID);


        for (int i = 1; i < 10; i++)
        {

        Param.Row newEleonaraRow = new(eleonara.First());

        Param.Column[] itemIds = newEleonaraRow.Cells.Take(Const.ItemLots).ToArray();
        Param.Column[] categories = newEleonaraRow.Cells.Skip(Const.CategoriesStart).Take(Const.ItemLots).ToArray();

        itemIds[0].SetValue(newEleonaraRow, 20760);
        categories[0].SetValue(newEleonaraRow, 1);


            newEleonaraRow.ID = 111600 + i;

            _itemLotParam_map.AddRow(newEleonaraRow);
        }
    }

    private void addPureBloodToLeyndellReplacingRuneArc()
    {
        IEnumerable<Param.Row> eleonara = _itemLotParam_map.Rows.Where(id => id.ID == 11000580);
        eleonara = eleonara.ToList();
        Debug.WriteLine(eleonara.First().ID);

        Param.Row newEleonaraRow = new(eleonara.First());

        Param.Column[] itemIds = newEleonaraRow.Cells.Take(Const.ItemLots).ToArray();
        Param.Column[] categories = newEleonaraRow.Cells.Skip(Const.CategoriesStart).Take(Const.ItemLots).ToArray();

        itemIds[0].SetValue(newEleonaraRow, 2160);
        categories[0].SetValue(newEleonaraRow, 1);

        newEleonaraRow.ID = 101622;

        _itemLotParam_map.AddRow(newEleonaraRow);
    }

    private void worldMap()
    {
        // Gives all map fragments
        IEnumerable<Param.Row> allMaps = _worldMapPieceParam.Rows;
        allMaps = allMaps.ToList();

        foreach (Param.Row row in allMaps)
        {
            Param.Column[] mapFields = row.Cells.Take(4).ToArray();
            mapFields.Last().SetValue(row, (uint)6001);
        }

        // Removes the map items from the world at the map locations
        List<int> mapFragmentOnMapIDs = new List<int>() { 12010000, 12010010, 12020060, 12030000, 12050000, 1034480200, 1036540500, 1037440210, 1038410200, 1040520500, 1042370200, 1042510500, 1044320000, 1045370020, 1048560700, 1049370500, 1049400500, 1049530700, 1052540700 };

        IEnumerable<Param.Row> allMapFragmentsOnMapIDs = _itemLotParam_map.Rows.Where(id => mapFragmentOnMapIDs.Contains(id.ID));
        allMapFragmentsOnMapIDs = allMapFragmentsOnMapIDs.ToList();

        foreach(Param.Row row in allMapFragmentsOnMapIDs)
        {
            Param.Column[] chance = row.Cells.Skip(Const.ChanceStart).Take(Const.ItemLots).ToArray();
            chance[0].SetValue(row, (ushort)0);
        }

        // Gives the option to change levels of elevations
        IEnumerable<Param.Row> undergroundMapFlag = _menuCommonParam.Rows.Where(id => id.ID == 0);
        undergroundMapFlag = undergroundMapFlag.ToList();

        Param.Column[] canShowUndergroundMap = undergroundMapFlag.First().Cells.Skip(22).Take(1).ToArray();
        canShowUndergroundMap.First().SetValue(undergroundMapFlag.First(), (uint)6001);

        Debug.WriteLine(canShowUndergroundMap.First().GetValue(undergroundMapFlag.First()));
    }

    private void randomizeStartingClassParams()
    {
        logItem("Starting Class Randomization");
        logItem($"Seed: {_seed}");
        logItem("Level estimate (x) appears if you cannot wield the weapon, assumes you are benefiting from two-handing.");

        List<Param.Row> staves = _weaponTypeDictionary[Const.StaffType];
        List<Param.Row> seals = _weaponTypeDictionary[Const.SealType];

        List<Param.Row> bows = _weaponTypeDictionary[Const.BowType];
        List<Param.Row> lightbows = _weaponTypeDictionary[Const.LightBowType];
        List<Param.Row> greatbows = _weaponTypeDictionary[Const.GreatbowType];
        List<Param.Row> ballistae = _weaponTypeDictionary[Const.BallistaType];
        List<Param.Row> crossbows = _weaponTypeDictionary[Const.CrossbowType];
        List<Param.Row> smallShields = _weaponTypeDictionary[Const.SmallShieldType];
        List<Param.Row> mediumShields = _weaponTypeDictionary[Const.MediumShieldType];
        List<Param.Row> greatShields = _weaponTypeDictionary[Const.GreatShieldType];
        List<Param.Row> spears = _weaponTypeDictionary[Const.SpearType];
        List<Param.Row> greatSpears = _weaponTypeDictionary[Const.GreatSpearType];
        List<Param.Row> claws = _weaponTypeDictionary[Const.ClawType];
        List<Param.Row> daggers = _weaponTypeDictionary[Const.DaggerType];
        List<Param.Row> fists = _weaponTypeDictionary[Const.FistType];
        List<Param.Row> colossalWeapons = _weaponTypeDictionary[Const.ColossalWeaponType];
        List<Param.Row> colossalSwords = _weaponTypeDictionary[Const.ColossalSwordType];

        IEnumerable<int> remembranceItems = _shopLineupParam.Rows.Where(r => r.ID is >= 101895 and <= 101948) // sword lance to Light of Miquella
            .Select(r => new ShopLineupParam(r).equipId);

        foreach (int items in remembranceItems)
        {
            Debug.WriteLine(items.ToString());
        }

        // washWeaponLevels  washWeaponMetadata  (washing only levels biases towards smithing weapons)
        List<int> mainArms = _weaponDictionary.Keys.Select(washWeaponLevels).Distinct()
            .Where(id => staves.All(s => s.ID != id) && seals.All(s => s.ID != id)
                && smallShields.All(s => s.ID != id)
                && mediumShields.All(s => s.ID != id)
                && greatShields.All(s => s.ID != id)
                && colossalWeapons.All(s => s.ID != id)
                && colossalSwords.All(s => s.ID != id)
                && spears.All(s => s.ID != id)
                && greatSpears.All(s => s.ID != id)
                && bows.All(s => s.ID != id)
                && lightbows.All(s => s.ID != id)
                && greatbows.All(s => s.ID != id)
                && ballistae.All(s => s.ID != id)
                && claws.All(s => s.ID != id)
                && daggers.All(s => s.ID != id)
                && fists.All(s => s.ID != id)
                && remembranceItems.All(i => i != id))
            .ToList();

        List<Param.Row> greatswords = _weaponTypeDictionary[Const.GreatswordType];
        List<Param.Row> curvedGreatswords = _weaponTypeDictionary[Const.CurvedGreatswordType];
        List<Param.Row> katanas = _weaponTypeDictionary[Const.KatanaType];
        List<Param.Row> twinblades = _weaponTypeDictionary[Const.TwinbladeType];
        List<Param.Row> heavyThrusting = _weaponTypeDictionary[Const.HeavyThrustingType];
        List<Param.Row> axes = _weaponTypeDictionary[Const.AxeType];
        List<Param.Row> greataxes = _weaponTypeDictionary[Const.GreataxeType];
        List<Param.Row> hammers = _weaponTypeDictionary[Const.HammerType];
        List<Param.Row> greatHammers = _weaponTypeDictionary[Const.GreatHammerType];
        List<Param.Row> halberds = _weaponTypeDictionary[Const.HalberdType];
        List<Param.Row> reapers = _weaponTypeDictionary[Const.ReaperType];
        List<Param.Row> greatKatanas = _weaponTypeDictionary[Const.GreatKatanaType];

        List<int> sideArms = _weaponDictionary.Keys.Select(washWeaponMetadata).Distinct()
            .Where(id => staves.All(s => s.ID != id) && seals.All(s => s.ID != id)
                && greatswords.All(s => s.ID != id)
                && curvedGreatswords.All(s => s.ID != id)
                && katanas.All(s => s.ID != id)
                && twinblades.All(s => s.ID != id)
                && heavyThrusting.All(s => s.ID != id)
                && axes.All(s => s.ID != id)
                && greataxes.All(s => s.ID != id)
                && hammers.All(s => s.ID != id)
                && greatHammers.All(s => s.ID != id)
                && greatSpears.All(s => s.ID != id)
                && halberds.All(s => s.ID != id)
                && reapers.All(s => s.ID != id)
                && greatKatanas.All(s => s.ID != id)
                && remembranceItems.All(i => i != id))
            .ToList();

        merchantWeaponList = _weaponDictionary.Keys.Select(washWeaponMetadata).Distinct()
            .Where(id => staves.All(s => s.ID != id) && seals.All(s => s.ID != id)
                && greatbows.All(s => s.ID != id)
                && greatShields.All(s => s.ID != id)
                && remembranceItems.All(i => i != id))
            .ToList();
        addDlcWeapons(merchantWeaponList); // used later for merchants

        for (int i = 0; i < Config.NumberOfClasses; i++)
        {
            Param.Row? row = _charaInitParam[Config.FirstClassId + i];
            if (row == null) { continue; }

            CharaInitParam startingClass = new(row);
            randomizeEquipment(startingClass, mainArms, sideArms);
            allocateStatsAndSpells(row.ID, startingClass);
            logCharaInitEntry(startingClass, i + 288100);
            addDescriptionString(startingClass, Const.ChrInfoMapping[i]);
        }
    }
    private void randomizeWeaponLocations()
    {
        OrderedDictionary chanceDictionary = new();
        OrderedDictionary guaranteedDictionary = new();
        // OrderedDictionary guaranteedArmor = new();

        IEnumerable<Param.Row> itemLotParamMap = _itemLotParam_map.Rows.Where(id => !Unk.unkItemLotParamMapWeapons.Contains(id.ID));
        IEnumerable<Param.Row> itemLotParamEnemy = _itemLotParam_enemy.Rows.Where(id => !Unk.unkItemLotParamEnemyWeapons.Contains(id.ID));
        IEnumerable<Param.Row> rowList = itemLotParamEnemy.Concat(itemLotParamMap);

        foreach (Param.Row row in rowList)
        {
            Param.Column[] itemIds = row.Cells.Take(Const.ItemLots).ToArray();
            Param.Column[] categories = row.Cells.Skip(Const.CategoriesStart).Take(Const.ItemLots).ToArray();
            Param.Column[] chances = row.Cells.Skip(Const.ChanceStart).Take(Const.ItemLots).ToArray();
            int totalWeight = chances.Sum(a => (ushort)a.GetValue(row));

            for (int i = 0; i < Const.ItemLots; i++)
            {
                int category = (int)categories[i].GetValue(row);
                int id = (int)itemIds[i].GetValue(row);
                int sanitizedId = washWeaponLevels(id); // for guaranteed wash method doesn't matter, for chance some have metadata

                // if (category == 3)
                // {
                //     addToOrderedDict(guaranteedArmor, 200, new ItemLotEntry(id, category));
                //     continue;
                // }

                if (category == Const.ItemLotWeaponCategory)
                {
                    if (!_weaponDictionary.TryGetValue(id, out EquipParamWeapon? wep)) { continue; }
                    if ((wep.wepType is Const.StaffType or Const.SealType)) { continue; }

                    ushort chance = (ushort)chances[i].GetValue(row);
                    if (chance == totalWeight)
                    {
                        addToOrderedDict(guaranteedDictionary, wep.wepType, new ItemLotEntry(id, category));
                        break; // Break here because the entire item lot param is just a single entry.
                    }
                    addToOrderedDict(chanceDictionary, wep.wepType, new ItemLotEntry(id, category));
                }
                if (category == Const.ItemLotCustomWeaponCategory)
                {
                    if (!_customWeaponDictionary.TryGetValue(id, out EquipParamWeapon? wep)) { continue; }
                    if (wep.wepType is Const.StaffType or Const.SealType) { continue; }

                    ushort chance = (ushort)chances[i].GetValue(row);
                    if (chance == totalWeight)
                    {
                        addToOrderedDict(guaranteedDictionary, wep.wepType, new ItemLotEntry(id, category));
                        break;
                    }
                    addToOrderedDict(chanceDictionary, wep.wepType, new ItemLotEntry(id, category));
                }
            }
        }

        // see: Randomizer.Helpers.cs
        addShopWeapons(guaranteedDictionary);
        addShopWeaponsByChance(chanceDictionary);

        removeDuplicateEntriesFrom(guaranteedDictionary);
        removeDuplicateEntriesFrom(chanceDictionary);
        groupArmaments(guaranteedDictionary);
        groupArmaments(chanceDictionary);
        Dictionary<int, ItemLotEntry> guaranteedReplacements = getRandomizedEntries(guaranteedDictionary);
        Dictionary<int, ItemLotEntry> chanceReplacements = getRandomizedEntries(chanceDictionary);

        logItem(">> Item Replacements - all instances of item on left will be replaced with item on right");
        logItem("## Guaranteed Weapons");
        logReplacementDictionary(guaranteedReplacements);
        logItem("\n## Chance Weapons");
        logReplacementDictionary(chanceReplacements);
        logItem("");

        foreach (Param.Row row in rowList)
        {
            Param.Column[] itemIds = row.Cells.Take(Const.ItemLots).ToArray();
            Param.Column[] categories = row.Cells.Skip(Const.CategoriesStart).Take(Const.ItemLots).ToArray();

            for (int i = 0; i < Const.ItemLots; i++)
            {
                int category = (int)categories[i].GetValue(row);
                int id = (int)itemIds[i].GetValue(row);

                // if (category == 3)
                // {
                //     if (armorReplacements.TryGetValue(id, out ItemLotEntry entry)) { itemIds[i].SetValue(row, entry.Id); }
                //     continue;
                // }

                if (category == Const.ItemLotWeaponCategory)
                {
                    int sanitizedId = washWeaponLevels(id);
                    if (!_weaponDictionary.TryGetValue(id, out _)) { continue; }

                    if (guaranteedReplacements.TryGetValue(id, out ItemLotEntry entry))
                    {
                        itemIds[i].SetValue(row, entry.Id);
                        categories[i].SetValue(row, entry.Category);
                        break;
                    }
                    if (chanceReplacements.TryGetValue(id, out entry))
                    {
                        itemIds[i].SetValue(row, entry.Id);
                        categories[i].SetValue(row, entry.Category);
                    }
                }
                if (category == Const.ItemLotCustomWeaponCategory)
                {
                    if (!_customWeaponDictionary.TryGetValue(id, out _)) { continue; }
                    if (guaranteedReplacements.TryGetValue(id, out ItemLotEntry entry))
                    {
                        itemIds[i].SetValue(row, entry.Id);
                        categories[i].SetValue(row, entry.Category);
                    }
                    if (!chanceReplacements.TryGetValue(id, out entry))
                    {
                        continue;
                    }
                    itemIds[i].SetValue(row, entry.Id);
                    categories[i].SetValue(row, entry.Category);
                }
            }
        }
    }
    private void randomizeShopLineupParam()
    {
        List<int> RemembranceWeaponIDs = new List<int>()
        {
            3100000, 3140000, 4020000, 4050000, 6040000, 8100000, 9020000, 11150000, 13030000,
            15040000, 15110000, 17010000,  20060000, 21060000, 23050000, 42000000, 3500000, 3510000,
            8500000, 17500000, 18510000, 23510000, 23520000, 67520000, 4530000, 4550000,
        };
        List<ShopLineupParam> shopLineupParamRemembranceList = new();

        foreach (Param.Row row in _shopLineupParam.Rows)
        {
            if ((byte)row["equipType"]!.Value.Value != Const.ShopLineupWeaponCategory || (row.ID < 101900 || row.ID > 101980))
            { continue; } // assures only weapons are randomized, maybe update for different armor logic, not sure

            ShopLineupParam lot = new(new Param.Row(row));
            int sanitizedId = washWeaponLevels(lot.equipId);

            if (!_weaponDictionary.TryGetValue(sanitizedId, out _)) { continue; }

            if (lot.equipId != sanitizedId)
            {
                _weaponNameDictionary[lot.equipId] = $"{_weaponNameDictionary[sanitizedId]} +{lot.equipId - sanitizedId}";
            }
            shopLineupParamRemembranceList.Add(lot);
        }

        logItem("<> Shop Replacements - Random item selected from pool of all weapons (not including infused weapons). Remembrances are randomized amongst each-other.");

        foreach (Param.Row row in _shopLineupParam.Rows)
        {
            if ((byte)row["equipType"]!.Value.Value != Const.ShopLineupWeaponCategory || row.ID > 101980) // TODO find out what this row.ID is, removes ~20 lots
            { continue; }

            ShopLineupParam lot = new(row);

            if (lot.equipId == Const.CarianRegalScepter || lot.equipId == Const.RellanaTwinBlades) // easier on players to have Rennala gift a usable weapon for square
            { lot.equipId = 9020000; } // randomizes Rennala's staff and Rellana's Twin Blades (new DLC weapon types have a bug not being randomized as expected)

            if (!_weaponDictionary.TryGetValue(washWeaponLevels(lot.equipId), out EquipParamWeapon? wep)) { continue; }

            if (!(wep.wepType is Const.StaffType or Const.SealType))
            {
                if (lot.mtrlId == -1) { replaceWeaponLineupParam(lot, merchantWeaponList); }
                else { replaceRemembranceLineupParam(lot, RemembranceWeaponIDs); }  // remembrance list is small, better to have seperate unique allocation logic
            }
        }
    }
    private void randomizeShopLineupParamMagic()
    {
        OrderedDictionary magicCategoryDictMap = new();
        List<ShopLineupParam> shopLineupParamRemembranceList = new();
        List<ShopLineupParam> shopLineupParamDragonList = new();

        foreach (Param.Row row in _shopLineupParam.Rows)
        {
            if ((byte)row["equipType"]!.Value.Value != Const.ShopLineupGoodsCategory || row.ID > 101980)
            { continue; } // Dragon Communion Shop 101950 - 101980

            ShopLineupParam lot = new(new Param.Row(row));
            if (!_magicDictionary.TryGetValue(lot.equipId, out Magic? magic)) { continue; }

            if (row.ID < 101950) // one row above Light of Miquella
            {
                if (lot.mtrlId == -1)
                {
                    addToOrderedDict(magicCategoryDictMap, magic.ezStateBehaviorType, lot.equipId);
                    continue;
                }
                shopLineupParamRemembranceList.Add(lot);
            }
            else
            { shopLineupParamDragonList.Add(lot); } // Dragon Communion Shop 101950 - 101980
        }

        foreach (Param.Row row in _itemLotParam_enemy.Rows.Concat(_itemLotParam_map.Rows))
        {
            Param.Column[] itemIds = row.Cells.Take(Const.ItemLots).ToArray();
            Param.Column[] categories = row.Cells.Skip(Const.CategoriesStart).Take(Const.ItemLots).ToArray();
            Param.Column[] chances = row.Cells.Skip(Const.ChanceStart).Take(Const.ItemLots).ToArray();
            int totalWeight = chances.Sum(a => (ushort)a.GetValue(row));
            for (int i = 0; i < Const.ItemLots; i++)
            {
                int category = (int)categories[i].GetValue(row);
                if (category != Const.ItemLotGoodsCategory) { continue; }

                int id = (int)itemIds[i].GetValue(row);
                if (!_magicDictionary.TryGetValue(id, out Magic? magic)) { continue; }

                ushort chance = (ushort)chances[i].GetValue(row);
                if (chance == totalWeight)
                {
                    addToOrderedDict(magicCategoryDictMap, magic.ezStateBehaviorType, id);
                    break;
                }
                addToOrderedDict(magicCategoryDictMap, magic.ezStateBehaviorType, id);
            }
        }
        removeDuplicateIntegersFrom(magicCategoryDictMap);

        Dictionary<int, int> magicShopReplacement = getRandomizedIntegers(magicCategoryDictMap);
        shopLineupParamRemembranceList.Shuffle(_random);
        shopLineupParamDragonList.Shuffle(_random);
        logItem("\n## All Magic Replacement.");
        logReplacementDictionaryMagic(magicShopReplacement);

        logItem("\n~* Shop Magic Replacement.");
        foreach (Param.Row row in _shopLineupParam.Rows)
        {
            logShopIdMagic(row.ID);
            if ((byte)row["equipType"]!.Value.Value != Const.ShopLineupGoodsCategory || row.ID > 101980)
            { continue; }

            ShopLineupParam lot = new(row);
            if (!_magicDictionary.TryGetValue(lot.equipId, out _)) { continue; }

            if (row.ID < 101950) // two up from Miquella's
            { replaceShopLineupParamMagic(lot, magicShopReplacement, shopLineupParamRemembranceList); }
            else
            {
                ShopLineupParam newDragonIncant = getNewId(lot.equipId, shopLineupParamDragonList);
                logItem($"{_goodsFmg[lot.equipId]} -> {_goodsFmg[newDragonIncant.equipId]}");
                copyShopLineupParam(lot, newDragonIncant);
            }
        }

        foreach (Param.Row row in _itemLotParam_enemy.Rows.Concat(_itemLotParam_map.Rows))
        {
            Param.Column[] itemIds = row.Cells.Take(Const.ItemLots).ToArray();
            Param.Column[] categories = row.Cells.Skip(Const.CategoriesStart).Take(Const.ItemLots).ToArray();
            for (int i = 0; i < Const.ItemLots; i++)
            {
                int category = (int)categories[i].GetValue(row);
                if (category != Const.ItemLotGoodsCategory) { continue; }

                int id = (int)itemIds[i].GetValue(row);
                if (!_magicDictionary.TryGetValue(id, out Magic _)) { continue; }
                if (!magicShopReplacement.TryGetValue(id, out int entry)) { continue; }

                itemIds[i].SetValue(row, entry);
            }
        }
    }
    private void randomizePerfumeBottleLocations()
    {
        // There are 9 pickup locations, choose 5 to be Perfume Bottle Weapon checks
        List<int> validLocationIDs = new List<int>()
        {
            // 11000130, // Leyndell chest
            // 11000470, // Leyndell path to grand lift
            16000110, // Volcano manor
            31180000, // Perfumer's Grotto
            1036510020, // Perfumer's Ruins (near Omenkiller)
            // 1036520070, // Perfumer's Ruins (on ledge)
            // 1039510000, // Altus by omen
            1039540040, // Shaded Castle
            1048380010 // Caelid
        };
        List<int> perfumeBottleIDs = new List<int>()
        {
            61500000, // Firespark
            61510000, // Chilling Mist
            61520000, // Frenzy Flame
            61530000, // Lightning
            61540000, // Deadly Poison
        };
        perfumeBottleIDs.Shuffle(_random);

        IReadOnlyList<Param.Row> perfumeBottleLocations = _itemLotParam_map.Rows.Where(id => validLocationIDs.Contains(id.ID)).ToList();

        logItem("## Perfume Bottles");
        foreach (Param.Row row in perfumeBottleLocations)
        {
            Param.Column itemId = row.Cells.ElementAt(0);
            Param.Column category = row.Cells.ElementAt(8);

            // Pop a random perfume bottle (it was shuffled above)
            itemId.SetValue(row, perfumeBottleIDs.Pop());
            // Set the item drop type to weapon
            category.SetValue(row, 2);
            // logItem($"{(int)itemId.GetValue(row)}");
            // logItem($"{(int)category.GetValue(row)}");
        }
    }
    private void replaceWeaponLineupParam(ShopLineupParam lot, List<int> WeaponShopList)
    {
        int newId = 0;
        do
        {
            _cancellationToken.ThrowIfCancellationRequested();
            int index = _random.Next(WeaponShopList.Count);
            newId = washWeaponMetadata(WeaponShopList[index]);
        } while (allocatedIDs.Contains(newId));

        lot.equipId = newId;
        allocatedIDs.Add(newId);
    }
    private void replaceRemembranceLineupParam(ShopLineupParam lot, IList<int> remembranceList)
    {
        int index = _random.Next(remembranceList.Count);
        int newId = remembranceList[index];
        logItem($"{_weaponNameDictionary[lot.equipId]} --> {newId}");
        remembranceList.Remove(newId);
        lot.equipId = newId;
    }
    private void randomizeShopArmorParam()
    {   // need the id's to identify the item lots
        List<int> baseHeadProtectors = new List<int>()
        {
            40000, 160000, 210000, 280000, 620000, 630000, 660000, 670000, 730000, 870000,
            880000, 890000, 1401000, 1500000, 1100000,
        };
        List<int> baseArmProtectors = new List<int>() // arm protectors found in shops
        {
            40200, 210200, 280200, 630200, 660200, 670200, 730200, 870200, 880200, 930200, 1500200,
        };
        List<int> baseBodyProtectors = new List<int>()
        {
            40100, 210100, 280100, 622100, 630100, 660100, 670100, 730100, 870100, 880100, 890100, 931100,
            962100, 1500100, 1102100, 1100100,
        };
        List<int> baseLegProtectors = new List<int>()
        {
            40300, 210300, 280300, 620300, 630300, 660300, 670300, 730300, 870300, 880300, 890300, 930300,
            960300, 1500300,
        };

        foreach (Param.Row row in _shopLineupParam.Rows)
        {
            if ((byte)row["equipType"]!.Value.Value == Const.ShopLineupArmorCategory)
            {
                if (row.ID > 101980) { continue; }

                ShopLineupParam lot = new(row);

                if (baseHeadProtectors.Contains(lot.equipId)) // chain coif (helmet)
                {
                    int index = _random.Next(Equipment.HeadArmorIDs.Count);
                    lot.equipId = Equipment.HeadArmorIDs[index];
                }
                if (baseBodyProtectors.Contains(lot.equipId)) // Chain armor
                {
                    int index = _random.Next(Equipment.BodyArmorIDs.Count);
                    lot.equipId = Equipment.BodyArmorIDs[index];
                }
                if (baseArmProtectors.Contains(lot.equipId))
                {
                    int index = _random.Next(Equipment.ArmsArmorIDs.Count);
                    lot.equipId = Equipment.ArmsArmorIDs[index];
                }
                if (lot.equipId == 1100300) // Chain Leggings
                {
                    int index = _random.Next(Equipment.LegsArmorIDs.Count);
                    lot.equipId = Equipment.LegsArmorIDs[index];
                }
            }
        }
    }
    private void patchAtkParam()
    {
        Param.Row swarmOfFlies1 = _atkParam_Pc[72100] ?? throw new InvalidOperationException("Entry 72100 not found in AtkParam_Pc");
        Param.Row swarmOfFlies2 = _atkParam_Pc[72101] ?? throw new InvalidOperationException("Entry 72101 not found in AtkParam_Pc");

        AtkParam swarmAtkParam1 = new(swarmOfFlies1);
        AtkParam swarmAtkParam2 = new(swarmOfFlies2);
        patchSpEffectAtkPowerCorrectRate(swarmAtkParam1);
        patchSpEffectAtkPowerCorrectRate(swarmAtkParam2);
    }

    private void patchSmithingStones()
    {
        // int adjustments = 0;
        foreach (Param.Row row in _equipMtrlSetParam.Rows)
        {
            int numberRequired = (sbyte)row["itemNum01"]!.Value.Value;
            int category = (byte)row["materialCate01"]!.Value.Value;
            int id = (int)row["materialId01"]!.Value.Value;
            sbyte three = 3;

            if (numberRequired > 1 && category == 4 && id >= 10100 && id < 10110)
            {
                // ++adjustments;
                // if (adjustments > 9) { row["itemNum01"]!.Value.SetValue(Const.ReducedSmithingCost); }
                // else
                // {
                row["itemNum01"]!.Value.SetValue(three);
                // }
            }
        }
    }

}
