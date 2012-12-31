using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Menu;

namespace Vocaluxe.PartyModes
{
    public enum ESongSource
    {
        TR_ALLSONGS,
        TR_CATEGORY,
        TR_PLAYLIST
    }

    public enum EPartyGameMode
    {
        TR_GAMEMODE_NORMAL,
        TR_GAMEMODE_SHORTSONG,
        TR_GAMEMODE_DUET
    }

    public class Round
    {
        public int SongID;
        public int SingerTeam1;
        public int SingerTeam2;
        public int PointsTeam1;
        public int PointsTeam2;
        public int Winner;
        public bool Finished = false;
    }
    #region Communication
    #region ToScreen
    public struct DataToScreenConfig
    {
        public int NumPlayerTeam1;
        public int NumPlayerTeam2;
        public int NumFields;
        public ESongSource SongSource;
        public int CategoryID;
        public int PlaylistID;
        public EPartyGameMode GameMode;
    }

    public struct DataToScreenNames
    {
        public int NumPlayerTeam1;
        public int NumPlayerTeam2;
        public List<int> ProfileIDsTeam1;
        public List<int> ProfileIDsTeam2;
    }

    public struct DataToScreenMain
    {
        public int CurrentRoundNr;
        public int Team;
        public int NumPlayerTeam1;
        public int NumPlayerTeam2;
        public int NumFields;
        public List<Round> Rounds;
        public List<int> Songs;
        public List<int> ProfileIDsTeam1;
        public List<int> ProfileIDsTeam2;
        public int[] NumJokerRandom;
        public int[] NumJokerRetry;
    }

    public struct DataFromScreen
    {
        public FromScreenConfig ScreenConfig;
        public FromScreenNames ScreenNames;
        public FromScreenMain ScreenMain;
    }

    public struct FromScreenConfig
    {
        public int NumPlayerTeam1;
        public int NumPlayerTeam2;
        public int NumFields;
        public ESongSource SongSource;
        public int CategoryID;
        public int PlaylistID;
        public EPartyGameMode GameMode;
    }

    public struct FromScreenNames
    {
        public bool FadeToConfig;
        public bool FadeToMain;
        public List<int> ProfileIDsTeam1;
        public List<int> ProfileIDsTeam2;
    }

    public struct FromScreenMain
    {
        public bool FadeToSinging;
        public bool FadeToNameSelection;
        public List<Round> Rounds;
        public int SingRoundNr;
        public List<int> Songs;
    }
    #endregion FromScreen
    #endregion Communication

    public sealed class PartyModeTicTacToe : CPartyMode
    {
        private const int MaxPlayer = 20;
        private const int MinPlayer = 2;
        private const int MaxTeams = 2;
        private const int MinTeams = 2;
        private const int NumFields = 9;

        enum EStage
        {
            NotStarted,
            Config,
            Names,
            Main,
            Singing
        }

        struct Data
        {
            public int NumPlayerTeam1;
            public int NumPlayerTeam2;
            public int NumFields;
            public int Team;
            public List<int> ProfileIDsTeam1;
            public List<int> ProfileIDsTeam2;

            public ESongSource SongSource;
            public int CategoryID;
            public int PlaylistID;

            public EPartyGameMode GameMode;

            public List<Round> Rounds;
            public List<int> Songs;

            public int CurrentRoundNr;
            public int SingRoundNr;

            public int[] NumJokerRandom;
            public int[] NumJokerRetry;
        }

        private DataToScreenConfig ToScreenConfig;
        private DataToScreenNames ToScreenNames;
        private DataToScreenMain ToScreenMain;

        private Data GameData;
        private EStage _Stage;

        public PartyModeTicTacToe()
        {
            _ScreenSongOptions.Selection.RandomOnly = false;
            _ScreenSongOptions.Selection.PartyMode = true;
            _ScreenSongOptions.Selection.CategoryChangeAllowed = true;
            _ScreenSongOptions.Selection.NumJokers = new int[] { 5, 5 };
            _ScreenSongOptions.Selection.TeamNames = new string[] { "foo", "bar" };

            _ScreenSongOptions.Sorting.SearchString = String.Empty;
            _ScreenSongOptions.Sorting.SearchActive = false;
            _ScreenSongOptions.Sorting.ShowDuetSongs = false;
            
            _Stage = EStage.NotStarted;

            ToScreenConfig = new DataToScreenConfig();
            ToScreenNames = new DataToScreenNames();
            ToScreenMain = new DataToScreenMain();

            GameData = new Data();
            GameData.NumFields = 9;
            GameData.NumPlayerTeam1 = 2;
            GameData.NumPlayerTeam2 = 2;
            GameData.ProfileIDsTeam1 = new List<int>();
            GameData.ProfileIDsTeam2 = new List<int>();
            GameData.CurrentRoundNr = 0;
            GameData.SingRoundNr = 0;
            GameData.SongSource = ESongSource.TR_ALLSONGS;
            GameData.PlaylistID = 0;
            GameData.CategoryID = 0;
            GameData.GameMode = EPartyGameMode.TR_GAMEMODE_NORMAL;
            GameData.Rounds = new List<Round>();
            GameData.Songs = new List<int>();
            GameData.NumJokerRandom = new int[2];
            GameData.NumJokerRetry = new int[2];
        }

        public override bool Init()
        {
            _Stage = EStage.NotStarted;

            _ScreenSongOptions.Sorting.IgnoreArticles = _Base.Config.GetIgnoreArticles();
            _ScreenSongOptions.Sorting.SongSorting = _Base.Config.GetSongSorting();
            _ScreenSongOptions.Sorting.Tabs = EOffOn.TR_CONFIG_OFF;
            _ScreenSongOptions.Selection.SongIndex = -1;

            if (_Base.Config.GetTabs() == EOffOn.TR_CONFIG_ON && _ScreenSongOptions.Sorting.SongSorting != ESongSorting.TR_CONFIG_NONE)
                _ScreenSongOptions.Sorting.Tabs = EOffOn.TR_CONFIG_ON;

            ToScreenMain.Rounds = new List<Round>();
            ToScreenMain.Songs = new List<int>();
            GameData.Songs = new List<int>();
            GameData.Rounds = new List<Round>();
            return true;
        }

        public override void DataFromScreen(string ScreenName, Object Data)
        {
            DataFromScreen data = new DataFromScreen();
            switch (ScreenName)
            {
                case "PartyScreenTicTacToeConfig":
                    
                    try
                    {
                        data = (DataFromScreen)Data;
                        GameData.NumPlayerTeam1 = data.ScreenConfig.NumPlayerTeam1;
                        GameData.NumPlayerTeam2 = data.ScreenConfig.NumPlayerTeam2;
                        GameData.NumFields = data.ScreenConfig.NumFields;
                        GameData.SongSource = data.ScreenConfig.SongSource;
                        GameData.CategoryID = data.ScreenConfig.CategoryID;
                        GameData.PlaylistID = data.ScreenConfig.PlaylistID;
                        GameData.GameMode = data.ScreenConfig.GameMode;

                        _Stage = EStage.Config;
                        _Base.Graphics.FadeTo(EScreens.ScreenPartyDummy);
                    }
                    catch (Exception e)
                    {
                        _Base.Log.LogError("Error in party mode TicTacToe. Can't cast received data from screen " + ScreenName + ". " + e.Message);
                    }
                    break;

                case "PartyScreenTicTacToeNames":
                    try
                    {
                        data = (DataFromScreen)Data;
                        if (data.ScreenNames.FadeToConfig)
                            _Stage = EStage.NotStarted;
                        else
                        {
                            if (_Base.Game.GetRandom(100) < 50)
                                GameData.Team = 0;
                            else
                                GameData.Team = 1;
                            GameData.ProfileIDsTeam1 = data.ScreenNames.ProfileIDsTeam1;
                            GameData.ProfileIDsTeam2 = data.ScreenNames.ProfileIDsTeam2;
                            _Stage = EStage.Names;
                        }

                        _Base.Graphics.FadeTo(EScreens.ScreenPartyDummy);
                    }
                    catch (Exception e)
                    {
                        _Base.Log.LogError("Error in party mode TicTacToe. Can't cast received data from screen " + ScreenName + ". " + e.Message);
                    }
                    break;

                case "PartyScreenTicTacToeMain":
                    try
                    {
                        data = (DataFromScreen)Data;
                        if (data.ScreenMain.FadeToSinging)
                        {
                            _Stage = EStage.Singing;
                            GameData.Rounds = data.ScreenMain.Rounds;
                            GameData.SingRoundNr = data.ScreenMain.SingRoundNr;
                            GameData.Songs = data.ScreenMain.Songs;
                        }
                        if (data.ScreenMain.FadeToNameSelection)
                        {
                            _Stage = EStage.Config;
                        }
                    }
                    catch (Exception e)
                    {
                        _Base.Log.LogError("Error in party mode TicTacToe. Can't cast received data from screen " + ScreenName + ". " + e.Message);
                    }


                    if (_Stage == EStage.Singing)
                        StartRound(data.ScreenMain.SingRoundNr);
                    if (_Stage == EStage.Config)
                        _Base.Graphics.FadeTo(EScreens.ScreenPartyDummy);
                    break;

                default:
                    _Base.Log.LogError("Error in party mode TicTacToe. Wrong screen is sending: " + ScreenName);
                    break;
            }
        }

        public override void UpdateGame()
        {
            /*
            if (_Base.Songs.GetCurrentCategoryIndex() != -1 || _ScreenSongOptions.Sorting.Tabs == EOffOn.TR_CONFIG_OFF)
                _ScreenSongOptions.Selection.RandomOnly = true;
            else
                _ScreenSongOptions.Selection.RandomOnly = false;*/
        }

        public override CMenuParty GetNextPartyScreen(out EScreens AlternativeScreen)
        {
            CMenuParty Screen = null;
            AlternativeScreen = EScreens.ScreenSong;

            switch (_Stage)
            {
                case EStage.NotStarted:
                    _Screens.TryGetValue("PartyScreenTicTacToeConfig", out Screen);
                    if (_Screens != null)
                    {
                        ToScreenConfig.NumPlayerTeam1 = GameData.NumPlayerTeam1;
                        ToScreenConfig.NumPlayerTeam2 = GameData.NumPlayerTeam2;
                        ToScreenConfig.NumFields = GameData.NumFields;
                        ToScreenConfig.SongSource = GameData.SongSource;
                        ToScreenConfig.CategoryID = GameData.CategoryID;
                        ToScreenConfig.PlaylistID = GameData.PlaylistID;
                        ToScreenConfig.GameMode = GameData.GameMode;
                        Screen.DataToScreen(ToScreenConfig);
                    }
                    break;
                case EStage.Config:
                    _Screens.TryGetValue("PartyScreenTicTacToeNames", out Screen);
                    if (_Screens != null)
                    {
                        ToScreenNames.NumPlayerTeam1 = GameData.NumPlayerTeam1;
                        ToScreenNames.NumPlayerTeam2 = GameData.NumPlayerTeam2;
                        ToScreenNames.ProfileIDsTeam1 = GameData.ProfileIDsTeam1;
                        ToScreenNames.ProfileIDsTeam2 = GameData.ProfileIDsTeam2;
                        Screen.DataToScreen(ToScreenNames);
                    }
                    break;
                case EStage.Names:
                    _Screens.TryGetValue("PartyScreenTicTacToeMain", out Screen);
                    if (_Screens != null)
                    {
                        if (GameData.Team == 1)
                            GameData.Team = 0;
                        else
                            GameData.Team = 1;
                        _Base.Songs.ResetPartySongSung();
                        GameData.CurrentRoundNr = 1;
                        ToScreenMain.CurrentRoundNr = 1;
                        ToScreenMain.NumPlayerTeam1 = GameData.NumPlayerTeam1;
                        ToScreenMain.NumPlayerTeam2 = GameData.NumPlayerTeam2;
                        ToScreenMain.NumFields = GameData.NumFields;
                        ToScreenMain.ProfileIDsTeam1 = GameData.ProfileIDsTeam1;
                        ToScreenMain.ProfileIDsTeam2 = GameData.ProfileIDsTeam2;
                        CreateRounds();
                        PrepareSongList();
                        SetNumJokers();
                        ToScreenMain.Rounds = GameData.Rounds;
                        ToScreenMain.Songs = GameData.Songs;
                        ToScreenMain.NumJokerRandom = GameData.NumJokerRandom;
                        ToScreenMain.NumJokerRetry = GameData.NumJokerRetry;
                        ToScreenMain.Team = GameData.Team;
                        Screen.DataToScreen(ToScreenMain);
                    }
                    break;
                case EStage.Main:
                    //nothing to do
                    break;
                case EStage.Singing:
                    _Screens.TryGetValue("PartyScreenTicTacToeMain", out Screen);
                    if (_Screens != null)
                    {
                        if (GameData.Team == 1)
                            GameData.Team = 0;
                        else
                            GameData.Team = 1;
                        ToScreenMain.CurrentRoundNr = GameData.CurrentRoundNr;
                        ToScreenMain.NumPlayerTeam1 = GameData.NumPlayerTeam1;
                        ToScreenMain.NumPlayerTeam2 = GameData.NumPlayerTeam2;
                        ToScreenMain.NumFields = GameData.NumFields;
                        ToScreenMain.ProfileIDsTeam1 = GameData.ProfileIDsTeam1;
                        ToScreenMain.ProfileIDsTeam2 = GameData.ProfileIDsTeam2;
                        ToScreenMain.Rounds = GameData.Rounds;
                        ToScreenMain.Songs = GameData.Songs;
                        ToScreenMain.NumJokerRandom = GameData.NumJokerRandom;
                        ToScreenMain.NumJokerRetry = GameData.NumJokerRetry;
                        ToScreenMain.Team = GameData.Team;
                        Screen.DataToScreen(ToScreenMain);
                    }
                    break;
                default:
                    break;
            }
            
            return Screen;
        }

        public override EScreens GetStartScreen()
        {
            return EScreens.ScreenPartyDummy;
        }

        public override EScreens GetMainScreen()
        {
            return EScreens.ScreenPartyDummy;
        }

        public override ScreenSongOptions GetScreenSongOptions()
        {
            _ScreenSongOptions.Sorting.SongSorting = _Base.Config.GetSongSorting();
            _ScreenSongOptions.Sorting.Tabs = _Base.Config.GetTabs();
            _ScreenSongOptions.Sorting.IgnoreArticles = _Base.Config.GetIgnoreArticles();

            return _ScreenSongOptions;
        }

        public override void OnSongChange(int SongIndex, ref ScreenSongOptions ScreenSongOptions)
        {
            ScreenSongOptions = _ScreenSongOptions;
        }

        public override void OnCategoryChange(int CategoryIndex, ref ScreenSongOptions ScreenSongOptions)
        {
            ScreenSongOptions = _ScreenSongOptions;
        }

        public override void SetSearchString(string SearchString, bool Visible)
        {
            _ScreenSongOptions.Sorting.SearchString = SearchString;
            _ScreenSongOptions.Sorting.SearchActive = Visible;
        }

        public override int GetMaxPlayer()
        {
            return MaxPlayer;
        }

        public override int GetMinPlayer()
        {
            return MinPlayer;
        }

        public override int GetMaxTeams()
        {
            return MaxTeams;
        }

        public override int GetMinTeams()
        {
            return MinTeams;
        }

        public override int GetMaxNumRounds()
        {
            return NumFields;
        }

        public override void JokerUsed(int TeamNr)
        {
        }

        public override void SongSelected(int SongID)
        {
            EGameMode gm = EGameMode.TR_GAMEMODE_NORMAL;

            switch (GameData.GameMode)
            {
                case EPartyGameMode.TR_GAMEMODE_NORMAL:
                    gm = EGameMode.TR_GAMEMODE_NORMAL;
                    break;

                case EPartyGameMode.TR_GAMEMODE_DUET:
                    gm = EGameMode.TR_GAMEMODE_DUET;
                    break;

                case EPartyGameMode.TR_GAMEMODE_SHORTSONG:
                    gm = EGameMode.TR_GAMEMODE_SHORTSONG;
                    break;
            }
            _Base.Game.AddSong(SongID, gm);

            _Base.Songs.AddPartySongSung(SongID);
            _Base.Graphics.FadeTo(EScreens.ScreenSing);
        }

        public override void LeavingHighscore()
        {
            UpdateScores();
            _Base.Graphics.FadeTo(EScreens.ScreenPartyDummy);
        }

        private void CreateRounds()
        {
            GameData.Rounds = new List<Round>();
            //Prepare Player-IDs
            List<int> Team1 = new List<int>();
            List<int> Team2 = new List<int>();
            List<int> IDs1 = new List<int>();
            List<int> IDs2 = new List<int>();
            //Add IDs to team-list
            while (Team1.Count <= GameData.NumFields)
            {
                if (IDs1.Count == 0)
                    for (int i = 0; i < GameData.NumPlayerTeam1; i++)
                        IDs1.Add(i);
                if (IDs2.Count == 0)
                    for (int i = 0; i < GameData.NumPlayerTeam2; i++)
                        IDs2.Add(i);
                if (Team1.Count <= GameData.NumFields)
                {
                    Team1.Add(IDs1[_Base.Game.GetRandom(IDs1.Count - 1)]);
                    IDs1.Remove(Team1[Team1.Count - 1]);
                }   
                if (Team2.Count <= GameData.NumFields)
                {
                    Team2.Add(IDs2[_Base.Game.GetRandom(IDs2.Count - 1)]);
                    IDs2.Remove(Team2[Team2.Count - 1]);
                }
            }

            for (int i = 0; i < GameData.NumFields; i++)
            {
                Round r = new Round();
                r.SingerTeam1 = Team1[i];
                r.SingerTeam2 = Team2[i];
                GameData.Rounds.Add(r);
            }
        }

        private void PrepareSongList()
        {
            EGameMode gm = EGameMode.TR_GAMEMODE_NORMAL;

            switch (GameData.GameMode)
            {
                case EPartyGameMode.TR_GAMEMODE_NORMAL:
                    gm = EGameMode.TR_GAMEMODE_NORMAL;
                    break;

                case EPartyGameMode.TR_GAMEMODE_DUET:
                    gm = EGameMode.TR_GAMEMODE_DUET;
                    break;

                case EPartyGameMode.TR_GAMEMODE_SHORTSONG:
                    gm = EGameMode.TR_GAMEMODE_SHORTSONG;
                    break;
            }

            while(GameData.Songs.Count < (GameData.NumFields + GameData.NumJokerRandom[0] + GameData.NumJokerRandom[1])){
                List<int> Songs = new List<int>();
                switch (GameData.SongSource)
                {
                    case ESongSource.TR_PLAYLIST:
                        for (int i = 0; i < _Base.Playlist.GetPlaylistSongCount(GameData.PlaylistID); i++)
                        {
                            int id = _Base.Playlist.GetPlaylistSong(GameData.PlaylistID, i).SongID;
                            foreach (EGameMode mode in _Base.Songs.GetSongByID(id).AvailableGameModes)
                                if (mode == gm) 
                                    Songs.Add(id);
                        }
                        break;

                    case ESongSource.TR_ALLSONGS:
                        for (int i = 0; i < _Base.Songs.GetNumSongs(); i++)
                        {
                            foreach (EGameMode mode in _Base.Songs.GetSongByID(i).AvailableGameModes)
                                if (mode == gm) 
                                    Songs.Add(i);
                        }
                        break;

                    case ESongSource.TR_CATEGORY:
                        _Base.Songs.SetCategory(GameData.CategoryID);
                        for (int i = 0; i < _Base.Songs.NumSongsInCategory(GameData.CategoryID); i++)
                        {
                            foreach(EGameMode mode in _Base.Songs.GetVisibleSong(i).AvailableGameModes)
                                if(mode == gm) 
                                    Songs.Add(_Base.Songs.GetVisibleSong(i).ID);
                        }
                        _Base.Songs.SetCategory(-1);
                        break;
                }
                while(Songs.Count > 0)
                {
                    GameData.Songs.Add(Songs[_Base.Game.GetRandom(Songs.Count - 1)]);
                    Songs.Remove(GameData.Songs[GameData.Songs.Count - 1]);
                }
            }
        }

        private void StartRound(int RoundNr)
        {
            _Base.Game.Reset();
            _Base.Game.ClearSongs();

            _Base.Game.SetNumPlayer(2);

            SPlayer[] player = _Base.Game.GetPlayer();
            if (player == null)
                return;

            if (player.Length < 2)
                return;

            SProfile[] profiles = _Base.Profiles.GetProfiles();
            Round r = GameData.Rounds[RoundNr];

            for (int i = 0; i < 2; i++)
            {
                //default values
                player[i].Name = "foobar";
                player[i].Difficulty = EGameDifficulty.TR_CONFIG_EASY;
                player[i].ProfileID = -1;
            }

            //try to fill with the right data
            if (r != null)
            {
                if (GameData.ProfileIDsTeam1[r.SingerTeam1] < profiles.Length)
                {
                    player[0].Name = profiles[GameData.ProfileIDsTeam1[r.SingerTeam1]].PlayerName;
                    player[0].Difficulty = profiles[GameData.ProfileIDsTeam1[r.SingerTeam1]].Difficulty;
                    player[0].ProfileID = GameData.ProfileIDsTeam1[r.SingerTeam1];
                    if (GameData.GameMode == EPartyGameMode.TR_GAMEMODE_DUET)
                        player[0].LineNr = 0;
                }
                if (GameData.ProfileIDsTeam2[r.SingerTeam2] < profiles.Length)
                {
                    player[1].Name = profiles[GameData.ProfileIDsTeam2[r.SingerTeam2]].PlayerName;
                    player[1].Difficulty = profiles[GameData.ProfileIDsTeam2[r.SingerTeam2]].Difficulty;
                    player[1].ProfileID = GameData.ProfileIDsTeam2[r.SingerTeam2];
                    if (GameData.GameMode == EPartyGameMode.TR_GAMEMODE_DUET)
                        player[1].LineNr = 1;
                }
                SongSelected(r.SongID);
            }
            else
                return;
        }

        private void SetNumJokers()
        {
            switch (GameData.NumFields)
            {
                case 9:
                    GameData.NumJokerRandom[0] = 1;
                    GameData.NumJokerRandom[1] = 1;
                    GameData.NumJokerRetry[0] = 0;
                    GameData.NumJokerRetry[1] = 0;
                    break;

                case 16:
                    GameData.NumJokerRandom[0] = 2;
                    GameData.NumJokerRandom[1] = 2;
                    GameData.NumJokerRetry[0] = 1;
                    GameData.NumJokerRetry[1] = 1;
                    break;

                case 25:
                    GameData.NumJokerRandom[0] = 3;
                    GameData.NumJokerRandom[1] = 3;
                    GameData.NumJokerRetry[0] = 2;
                    GameData.NumJokerRetry[1] = 2;
                    break;
            }
        }

        private void UpdateScores()
        {
            if (!GameData.Rounds[GameData.SingRoundNr].Finished)
                GameData.CurrentRoundNr++;

            SPlayer[] results = _Base.Game.GetPlayer();
            if (results == null)
                return;

            if (results.Length < 2)
                return;

            GameData.Rounds[GameData.SingRoundNr].PointsTeam1 = (int)Math.Round(results[0].Points);
            GameData.Rounds[GameData.SingRoundNr].PointsTeam2 = (int)Math.Round(results[1].Points);
            GameData.Rounds[GameData.SingRoundNr].Finished = true;
            if (GameData.Rounds[GameData.SingRoundNr].PointsTeam1 < GameData.Rounds[GameData.SingRoundNr].PointsTeam2)
            {
                GameData.Rounds[GameData.SingRoundNr].Winner = 2;
            }
            else if (GameData.Rounds[GameData.SingRoundNr].PointsTeam1 > GameData.Rounds[GameData.SingRoundNr].PointsTeam2)
            {
                GameData.Rounds[GameData.SingRoundNr].Winner = 1;
            }
            else
            {
                GameData.Rounds[GameData.SingRoundNr].Finished = false;
                GameData.CurrentRoundNr--;
            }
        }
    }
}