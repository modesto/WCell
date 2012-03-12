namespace WCell.Constants.World
{
    public enum WorldStateId
    {
        WSGAllianceScore = 1581,
        WSGHordeScore = 1582,
        WSGAlliancePickupState = 1545,
        WSGHordePickupState = 1546,
        WSGUnknown = 1547,
        WSGMaxScore = 1601,

        WSGHordeFlagState = 2338,
        WSGAllianceFlagState = 2339,
        // 1581 alliance flag captures
        // 8 1582 horde flag captures
        // 9 1545 unk, set to 1 on alliance flag pickup...
        // 10 1546 unk, set to 1 on horde flag pickup, after drop it's -1
        // 11 1547 unk
        // 13 2338 horde (0 - hide, 1 - flag ok, 2 - flag picked up (flashing), 3 - flag picked up (not flashing)
        // 14 2339 alliance (0 - hide, 1 - flag ok, 2 - flag picked up (flashing), 3 - flag picked up (not flashing)

        COTMedvihsShield = 2540,
        COTTimeRiftsOpened = 2784,

        LightHopeChapelBattleDKTimeRemaining = 3604,

        TheramoreMarksmanRemaining = 3082,
        /// <summary>
        /// Violet Hold
        /// </summary>
        PortalsOpened = 3810,
        PrisonSealIntegrity = 3815,

        UndercityBattleTimeToStart = 3877,

        WGHours = 3975,
        WG10Minutes = 3976,
        WGMinutes = 3782,
        WG10Seconds = 3784,
        WGSeconds = 3785,

        ABOccupiedBasesHorde = 1778,
        ABOccupiedBasesAlliance = 1779,
        ABResourcesAlliance = 1776,
        ABResourcesHorde = 1777,
        ABMaxResources = 1780,

        ABShowStableIcon = 1842,                    // Neutral
        ABShowStableIconAlliance = 1767,            // Alliance controlled
        ABShowStableIconHorde = 1768,               // Horde controlled
        ABShowStableIconAllianceContested = 1769,   // Alliance contested
        ABShowStableIconHordeContested = 1770,      // Horde contested
        
        ABShowGoldMineIcon = 1843,                  // Neutral
        ABShowGoldMineIconAlliance = 1787,          // Alliance controlled
        ABShowGoldMineIconHorde = 1788,             // Horde controlled
        ABShowGoldMineIconAllianceContested = 1789, // Alliance contested
        ABShowGoldMineIconHordeContested = 1790,    // Horde contested

        ABShowLumberMillIcon = 1844,                // Neutral
        ABShowLumberMillIconAlliance = 1792,        // Alliance controlled
        ABShowLumberMillIconHorde = 1793,           // Horde controlled
        ABShowLumberMillIconAllianceContested = 1794, // Alliance contested
        ABShowLumberMillIconHordeContested = 1795,  // Horde contested

        ABShowFarmIcon = 1845,                      // Neutral
        ABShowFarmIconAlliance = 1772,              // Alliance controlled
        ABShowFarmIconHorde = 1773,                 // Horde controlled
        ABShowFarmIconAllianceContested = 1774,     // Alliance contested
        ABShowFarmIconHordeContested = 1775,        // Horde contested

        ABShowBlacksmithIcon = 1846,                // Neutral
        ABShowBlacksmithIconAlliance = 1782,        // Alliance controlled
        ABShowBlacksmithIconHorde = 1783,           // Horde controlled
        ABShowBlacksmithIconAllianceContested = 1784, // Alliance contested
        ABShowBlacksmithIconHordeContested = 1785,  // Horde contested

        ABNearVictoryWarning = 1955,

        #region EOTS WorldStates
        EOTSMaxResources = 2751,
        EOTSResourcesHorde = 2750,
        EOTSResourcesAlliance = 2749,
        EOTSHordeBases = 2753,
        EOTSAllianceBases = 2752,
        
        EOTSMageTowerUncontrolled = 2728, //1=yes, 0=no
        EOTSMageTowerHordeContested = 2742,
        EOTSMageTowerAllianceContested = 2741,
        EOTSMageTowerHordeControl = 2729,
        EOTSMageTowerAllianceControl = 2730,

        EOTSFelReaverUncontrolled = 2725, //1=yes, 0=no
        EOTSFelReaverHordeContested = 2740,
        EOTSFelReaverAllianceContested = 2739,
        EOTSFelReaverHordeControl = 2727,
        EOTSFelReaverAllianceControl = 2726,

        EOTSDraeneiRuinsUncontrolled = 2731, //1=yes, 0=no
        EOTSDraeneiRuinsHordeContested = 2737,
        EOTSDraeneiRuinsAllianceContested = 2738,
        EOTSDraeneiRuinsHordeControl = 2733,
        EOTSDraeneiRuinsAllianceControl = 2732,

        EOTSBloodElfTowerUncontrolled = 2722, //1=yes, 0=no
        EOTSBloodElfTowerHordeContested = 2744,
        EOTSBloodElfTowerAllianceContested = 2743,
        EOTSBloodElfTowerHordeControl = 2724,
        EOTSBloodElfTowerAllianceControl = 2723,

        EOTSNearVictoryWarning = 3085,

        EOTSHordeStats = 2770, //(1 - show, 0 - hide) // 02 -> horde picked up the flag
        EOTSAllianceStats = 2769, //(1 - show, 0 - hide) // 02 -> alliance picked up the flag

        EOTSCaptureBarColors = 2720, //(100 -> empty (only grey), 0 -> blue|red (no grey), default 0)
        EOTSCaptureBarStatus = 2719, //(0 - left, 100 - right)
        EOTSCaptureBarVisable = 2718, //(1 - show, 0 - hide)
        
        /*2736 unk // 0 at start
        2735 unk // 0 at start
        2757 Flag (1 - show, 0 - hide) - doesn't work exactly this way!
        2565 unk, constant?*/
        #endregion

        AlgalonTimeToSignal = 4131,
        End
    }
}