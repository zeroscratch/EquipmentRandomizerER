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
using Org.BouncyCastle.Asn1.Mozilla;

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
        shuffleRemembrancesWeaponsWithRemembranceWeapons();
        _cancellationToken.ThrowIfCancellationRequested();
        randomizeShopArmorParam();
        _cancellationToken.ThrowIfCancellationRequested();
        randomizePerfumeBottleLocations();
        removeMohgGreatRuneAndRemembrance();
        patchAtkParam();
        patchSmithingStones();
        _cancellationToken.ThrowIfCancellationRequested();
        allocatedIDs = new HashSet<int>() { 2510000, };
        worldMap();
        addArcaneTalismanToTwinMaidenHust();
        addMapIconsForWorldMap();
        writeFiles();
        writeLog();
        SeedInfo = new SeedInfo(_seed, Util.GetShaRegulation256Hash());
        string seedJson = JsonSerializer.Serialize(SeedInfo);
        File.WriteAllText(Config.LastSeedPath, seedJson);
        return Task.CompletedTask;
    }

    private void removeMohgGreatRuneAndRemembrance()
    {
        Param.Row mohgRemembrance = _itemLotParam_map.Rows.Where(id => id.ID == 10120).ToArray()[0];
        Param.Column categoryForRemembrance = mohgRemembrance.Cells.ElementAt(Const.CategoriesStart);
        categoryForRemembrance.SetValue(mohgRemembrance, 0);

        Param.Row mohgGreatRune = _itemLotParam_map.Rows.Where(id => id.ID == 10121).ToArray()[0];
        Param.Column categoryForGreatRune = mohgGreatRune.Cells.ElementAt(Const.CategoriesStart);
        categoryForGreatRune.SetValue(mohgGreatRune, 0);

        Param.Row lordsRuneInMohgwyn = _itemLotParam_map.Rows.Where(id => id.ID == 12050690).ToArray()[0];
        Param.Column[] chance = lordsRuneInMohgwyn.Cells.Skip(Const.ChanceStart).Take(Const.ItemLots).ToArray();
        chance[0].SetValue(lordsRuneInMohgwyn, (ushort)0);
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

    private void addPurebloodToElenora()
    {
        IEnumerable<Param.Row> eleonara = _itemLotParam_map.Rows.Where(id => id.ID == 101620);
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

    private void addSmithingStonesToIji()
    {
        IEnumerable<Param.Row> somberStoneID = _shopLineupParam.Rows.Where(id => id.ID == 100225);
        somberStoneID = somberStoneID.ToList();

        int[] sellPricesForSmithingStones = { 300, 600, 900 };

        for(int i = 0; i < 3;i++)
        {
            Param.Row newSmithingStones = new(somberStoneID.First());

            Param.Column equipId = newSmithingStones.Cells.ElementAt(0);
            Param.Column equipType = newSmithingStones.Cells.ElementAt(7);
            Param.Column sellQuanity = newSmithingStones.Cells.ElementAt(5);
            Param.Column sellPrice = newSmithingStones.Cells.ElementAt(1);
            Param.Column eventFlagId = newSmithingStones.Cells.ElementAt(3);

            equipId.SetValue(newSmithingStones, 10100 + i);
            equipType.SetValue(newSmithingStones, (byte)3);
            sellQuanity.SetValue(newSmithingStones, (short)9);
            sellPrice.SetValue(newSmithingStones, sellPricesForSmithingStones[i]);
            eventFlagId.SetValue(newSmithingStones, (uint)(120300 + i * 10));

            newSmithingStones.ID = 100230 + i;

            _shopLineupParam.AddRow(newSmithingStones);

        }
    }

    private void addPureBloodToLeyndellReplacingRuneArc()
    {
        Param.Row runeArcItemLot = _itemLotParam_map.Rows.Where(id => id.ID == 11000580).ToArray()[0];

        Param.Column itemId = runeArcItemLot.Cells.ElementAt(0);
        Param.Column category = runeArcItemLot.Cells.ElementAt(Const.CategoriesStart);

        itemId.SetValue(runeArcItemLot, 2160);
        category.SetValue(runeArcItemLot, 1);
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
    }

    private void addMapIconsForWorldMap()
    {
        List<int> mapIcons = new List<int>() { 100000, 110000, 111000, 130000, 140000, 150000, 150001, 160000, 180000, 180001, 200000, 200001, 200100, 200101, 210000, 210101, 220000, 220001, 280000, 300000, 300100, 300200, 300300, 300400, 300500, 300600, 300700, 300800, 300900, 301000, 301100, 301200, 301200, 301300, 301400, 301500, 301600, 301700, 301800, 301900, 302000, 310000, 310100, 310200, 310300, 310400, 310500, 310600, 310700, 310900, 311000, 311100, 311200, 311500, 311700, 311800, 311900, 312000, 312100, 312200, 320000, 320100, 320200, 320400, 320500, 320700, 320800, 321100, 341000, 341100, 341101, 341200, 341201, 341300, 341400, 341500, 350000, 392000, 400000, 400100, 400200, 410000, 410100, 410200, 420000, 420100, 420200, 420300, 430000, 430100, 450000, 450100, 450200, 61413200, 61413300, 61413301, 61413500, 61413800, 61423200, 61423300, 61423400, 61423600, 61423700, 61423701, 61423702, 61423800, 61433100, 61433301, 61433400, 61433401, 61433600, 61443300, 61443302, 61443400, 61443401, 61443500, 61443600, 61443800, 61453300, 61453600, 61453700, 61453701, 61453702, 61453900, 61463600, 61463800, 62334000, 62334200, 62334300, 62334400, 62334500, 62334700, 62344100, 62344200, 62344300, 62344400, 62344800, 62345000, 62345001, 62345002, 62345100, 62354100, 62354200, 62354201, 62354400, 62354700, 62355000, 62364100, 62364300, 62364900, 62365000, 62374200, 62374400, 62374600, 62374900, 62384100, 62384200, 62384500, 62384600, 62384700, 62384800, 62384801, 62384900, 62393900, 62394100, 62394400, 62394800, 63355400, 63365100, 63365200, 63365203, 63375100, 63375200, 63375400, 63385000, 63385100, 63385200, 63385400, 63395000, 63395200, 63395300, 63395400, 63405200, 63405300, 63405500, 63415300, 63415301, 63415400, 63415500, 63415501, 63425400, 63435000, 63435300, 63445300, 64464000, 64464001, 64473800, 64473801, 64474000, 64474001, 64474002, 64483600, 64483800, 64484100, 64493800, 64493801, 64493900, 64494000, 64503800, 64503900, 64503901, 64513600, 64513700, 64513900, 64514000, 64514300, 64524100, 65475500, 65475800, 65485700, 65495300, 65495301, 65505600, 65505601, 65505602, 65515300, 65515600, 65515700, 65525500, 65525600, 65525700, 65535600, 65545300, 65545500, 66120100, 66120102, 66120103, 66120104, 66120200, 66120201, 66120202, 66120203, 66120500, 66120700, 68454100, 68454200, 68463800, 68464000, 68464001, 68464100, 68464200, 68464201, 68464300, 68464301, 68464400, 68474400, 68474401, 68483700, 68483800, 68484100, 68484101, 68484300, 68493900, 68494200, 68503800, 68504000, 68534100, 69464500, 69464501, 69464700, 69474500, 69474600, 69484500, 69494300, 69494301, 69494400, 69494500, 69494901, 69494902, 69504400, 69504401, 69504402, 69504600, 69504800, 69514400, 69514500, 69514501, 69514600, 69514601, 69514700, 69534600 };

        IEnumerable<Param.Row> worldMapPointParamIcons = _worldMapPointParam.Rows.Where(id => mapIcons.Contains(id.ID));
        worldMapPointParamIcons = worldMapPointParamIcons.ToList();

        foreach (Param.Row row in worldMapPointParamIcons)
        {
            Param.Column[] mapFields = row.Cells.Take(4).ToArray();
            mapFields.Last().SetValue(row, (uint)6001);
        }
    }

    private void addArcaneTalismanToTwinMaidenHust()
    {
        IEnumerable<Param.Row> hostTrickMirrorTalisman = _shopLineupParam.Rows.Where(id => id.ID == 101863);
        hostTrickMirrorTalisman = hostTrickMirrorTalisman.ToList();

        Param.Column equipId = hostTrickMirrorTalisman.First().Cells.ElementAt(0);
        equipId.SetValue(hostTrickMirrorTalisman.First(), 8020);
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

    // Shuffles Remembrance weapons with each other and sets the incants and scorceries category to 'Weapon' type. Also removes all costs
    private void shuffleRemembrancesWeaponsWithRemembranceWeapons()
    {
        List<int> targetRemembrances = new List<int>() { 101900, 101901, 101902, 101903, 101904, 101905, 101906, 101907, 101910, 101911, 101918, 101919, 101924, 101925 };
        Param.Row[] remembrances = _shopLineupParam.Rows.Where(id => targetRemembrances.Contains(id.ID)).ToArray();

        List<int> RemembranceWeaponIDs = new List<int>()
        {
            3100000, 3140000, 4020000, 4050000, 6040000, 8100000, 9020000, 11150000, 13030000,
            15040000, 15110000, 17010000,  20060000, 21060000, 23050000, 42000000, 3500000, 3510000,
            8500000, 17500000, 18510000, 23510000, 23520000, 67520000, 4530000, 4550000,
        };

        RemembranceWeaponIDs.Shuffle(_random);

        foreach (Param.Row row in remembrances)
        {
            Param.Column equipId = row.Cells.ElementAt(0);
            Param.Column category = row.Cells.ElementAt(7);
            Param.Column sellPrice = row.Cells.ElementAt(1);
            int newId = RemembranceWeaponIDs.Pop();
            equipId.SetValue(row, newId);
            category.SetValue(row, (byte)0);
            sellPrice.SetValue(row, 0);
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
           16000110, // Volcano manor
           31180000, // Perfumer's Grotto
           1036510020, // Perfumer's Ruins (near Omenkiller)
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

        // logItem("## Perfume Bottles");
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

        List<int> vanillaLocationsToRemoveIDs = new List<int>()
        {
            11000130, // Leyndell chest
            11000470, // Leyndell path to grand lift
            1036520070, // Perfumer's Ruins (on ledge)
            1039510000, // Altus by omen
        };
        IReadOnlyList<Param.Row> perfumeBottleLocationsToRemove = _itemLotParam_map.Rows.Where(id => vanillaLocationsToRemoveIDs.Contains(id.ID)).ToList();
        foreach (Param.Row row in perfumeBottleLocationsToRemove)
        {
            Param.Column itemId = row.Cells.ElementAt(0);
            Param.Column category = row.Cells.ElementAt(8);
            Param.Column chance = row.Cells.ElementAt(16);
            itemId.SetValue(row, 0);
            category.SetValue(row, 0);
            chance.SetValue(row, (ushort)0);
        }
        int merchantPerfumeBottleToRemove = 100725;
        Param.Row merchantPerfumeBottle = _shopLineupParam.Rows.Where(id => id.ID == merchantPerfumeBottleToRemove).ToArray()[0];
        Param.Column equipId = merchantPerfumeBottle.Cells.ElementAt(0);
        equipId.SetValue(merchantPerfumeBottle, 20760); // change it to a mushroom
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
            sbyte one = 1;

            if (numberRequired > 1 && category == 4 && id >= 10100 && id < 10110)
            {
                // ++adjustments;
                // if (adjustments > 9) { row["itemNum01"]!.Value.SetValue(Const.ReducedSmithingCost); }
                // else
                // {
                row["itemNum01"]!.Value.SetValue(one);
                // }
            }
        }
    }

}
