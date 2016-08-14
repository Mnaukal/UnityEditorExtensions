﻿namespace PlayFab.Editor
{
    using System;
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using System.Linq;

    [InitializeOnLoad]
    public class PlayFabEditorSettings : Editor
    {
        #region state variables
        public enum SubMenuStates
        {
            StandardSettings,
            ApiSettings,
            TitleSettings,
            Packages
        }

        public enum WebRequestType
        {
            UnityWww, // High compatability Unity api calls
            HttpWebRequest // High performance multi-threaded api calls
        }

        internal static List<string> buildTargets;
        private const string AdminAPI = "ENABLE_PLAYFABADMIN_API";
        private const string ServerAPI = "ENABLE_PLAYFABSERVER_API";
        private const string ClientAPI = "DISABLE_PLAYFABCLIENT_API";
        private const string DebugRequestTiming = "PLAYFAB_REQUEST_TIMING";

        private static bool _isAdminSdkEnabled;
        private static bool _isServerSdkEnabled;
        private static bool _isClientSdkEnabled = true;
        private static bool _IsDebugRequestTiming;

        private static SubMenuStates _subMenuState;

        //Settings properties
        private static string _TitleId;

        private static string[] titleOptions;
        private static string[] studioOptions;
       
        private static int _selectedTitleIdIndex = -1;
        private static int _selectedStudioIndex = -1;
        private static int _prevSelectedTitleIdIndex = -1;
        private static int _prevSelectedStudioIndex = -1;


#if ENABLE_PLAYFABADMIN_API || ENABLE_PLAYFABSERVER_API
        private static string _DeveloperSecretKey;
#endif
        private static WebRequestType _RequestType;
        private static int _RequestTimeOut;
        private static bool _KeepAlive;
        private static bool _CompressApiData = false;
        private static bool _EnableRealtimeLogging;
        private static string _LoggerHost;
        private static string _LoggerPort;
        private static int _LogCapLimit;
       

        private static bool _isSettingsSet = false;
        private static Dictionary<string, StudioDisplaySet > studioFoldOutStates = new Dictionary<string, StudioDisplaySet>();
        private static Vector2 TitleScrollPos = Vector2.zero;
        #endregion

        #region draw calls

        public static void DrawApiSubPanel()
        {

            float labelWidth = 160;

            GUILayout.BeginVertical(PlayFabEditorHelper.uiStyle.GetStyle("gpStyleGray1"));

            using (FixedWidthLabel fwl = new FixedWidthLabel("Enable Client API: "))
            {
                GUILayout.Space(labelWidth - fwl.fieldWidth);
                PlayFabEditorDataService.envDetails.isClientApiEnabled = EditorGUILayout.Toggle(PlayFabEditorDataService.envDetails.isClientApiEnabled, PlayFabEditorHelper.uiStyle.GetStyle("Toggle"), GUILayout.MinHeight(25));
            }

            using (FixedWidthLabel fwl = new FixedWidthLabel("Enable Admin API:  "))
            {
                GUILayout.Space(labelWidth - fwl.fieldWidth);
                PlayFabEditorDataService.envDetails.isAdminApiEnabled = EditorGUILayout.Toggle(PlayFabEditorDataService.envDetails.isAdminApiEnabled, PlayFabEditorHelper.uiStyle.GetStyle("Toggle"), GUILayout.MinHeight(25));
            }

            using (FixedWidthLabel fwl = new FixedWidthLabel("Enable Server API: "))
            {
                GUILayout.Space(labelWidth - fwl.fieldWidth);
                PlayFabEditorDataService.envDetails.isServerApiEnabled = EditorGUILayout.Toggle(PlayFabEditorDataService.envDetails.isServerApiEnabled , PlayFabEditorHelper.uiStyle.GetStyle("Toggle"), GUILayout.MinHeight(25));
            }

            using (FixedWidthLabel fwl = new FixedWidthLabel("Debug Request Times: "))
            {
                GUILayout.Space(labelWidth - fwl.fieldWidth);
                PlayFabEditorDataService.envDetails.isDebugRequestTimesEnabled = EditorGUILayout.Toggle(PlayFabEditorDataService.envDetails.isDebugRequestTimesEnabled, PlayFabEditorHelper.uiStyle.GetStyle("Toggle"), GUILayout.MinHeight(25));
            }

            GUILayout.EndVertical();
        }

        public static void DrawSettingsPanel()
        {
            SetSettingsProperties();
            if (EditorPrefs.HasKey("PLAYFAB_CURRENT_SETTINGSMENU"))
            {
                _subMenuState = (SubMenuStates)EditorPrefs.GetInt("PLAYFAB_CURRENT_SETTINGSMENU");
            }

            var apiSettingsButtonStyle = PlayFabEditorHelper.uiStyle.GetStyle("textButton");
            var standardSettingsButtonStyle = PlayFabEditorHelper.uiStyle.GetStyle("textButton");
            var titleSettingsButtonStyle = PlayFabEditorHelper.uiStyle.GetStyle("textButton");
            var packagesSettingsButtonStyle = PlayFabEditorHelper.uiStyle.GetStyle("textButton");

            if (_subMenuState == SubMenuStates.StandardSettings)
            {
                standardSettingsButtonStyle = PlayFabEditorHelper.uiStyle.GetStyle("textButton_selected");
            }
            else
            {
                standardSettingsButtonStyle = PlayFabEditorHelper.uiStyle.GetStyle("textButton");
            }

            if (_subMenuState == SubMenuStates.ApiSettings)
            {
                apiSettingsButtonStyle = PlayFabEditorHelper.uiStyle.GetStyle("textButton_selected");
            }
            else
            {
                apiSettingsButtonStyle = PlayFabEditorHelper.uiStyle.GetStyle("textButton");
            }

            if (_subMenuState == SubMenuStates.TitleSettings)
            {
                titleSettingsButtonStyle = PlayFabEditorHelper.uiStyle.GetStyle("textButton_selected");
            }
            else
            {
                titleSettingsButtonStyle = PlayFabEditorHelper.uiStyle.GetStyle("textButton");
            }

            GUILayout.BeginHorizontal(PlayFabEditorHelper.uiStyle.GetStyle("gpStyleGray1"), GUILayout.ExpandWidth(true));

            if (GUILayout.Button("PROJECT", standardSettingsButtonStyle))
            {
                OnStandardSetttingsClicked();
            }
            if (GUILayout.Button("STUDIOS", titleSettingsButtonStyle))
            {
                OnTitleSettingsClicked();
            }

            if (GUILayout.Button("API", apiSettingsButtonStyle))
            {
                OnApiSettingsClicked();
            }

//            if (GUILayout.Button("PACKAGES", PlayFabEditorHelper.uiStyle.GetStyle("textButton")))
//            {
//                //OnStandardSetttingsClicked();
//            }

            GUILayout.EndHorizontal();

            switch (_subMenuState)
            {
                case SubMenuStates.StandardSettings:
                    DrawStandardSettingsSubPanel();
                    break;
                case SubMenuStates.ApiSettings:
                    DrawApiSubPanel();
                    break;
                 case SubMenuStates.TitleSettings:
                    DrawTitleSettingsSubPanel();
                    break;
                 case SubMenuStates.Packages:
                    //DrawPackagesSubPanel();
                    break;
            }



        }


        public static void DrawTitleSettingsSubPanel()
        {
            float labelWidth = 100;

            // this probably does not need to run every update.

            if(PlayFabEditorDataService.accountDetails.studios.Count != studioFoldOutStates.Count)
            {
                studioFoldOutStates.Clear();
                foreach(var studio in PlayFabEditorDataService.accountDetails.studios)
                {
                    if(!studioFoldOutStates.ContainsKey(studio.Id))
                    {
                        studioFoldOutStates.Add(studio.Id, new StudioDisplaySet(){ Studio = studio });
                    }

                    foreach(var title in studio.Titles)
                    {
                        // studioFoldOutStates[studio.Id].titleFoldOutStates
                        if(!studioFoldOutStates[studio.Id].titleFoldOutStates.ContainsKey(title.Id))
                        {
                            studioFoldOutStates[studio.Id].titleFoldOutStates.Add(title.Id, new TitleDisplaySet(){ Title = title });
                        }
                    }
                }
             }

            
            TitleScrollPos = GUILayout.BeginScrollView(TitleScrollPos, PlayFabEditorHelper.uiStyle.GetStyle("gpStyleGray1"));

            GUILayout.BeginHorizontal(PlayFabEditorHelper.uiStyle.GetStyle("gpStyleClear"));
                EditorGUILayout.LabelField("STUDIOS:", PlayFabEditorHelper.uiStyle.GetStyle("labelStyle"), GUILayout.Width(labelWidth));
                GUILayout.FlexibleSpace();
                if(GUILayout.Button("REFRESH", PlayFabEditorHelper.uiStyle.GetStyle("Button")))
                {
                    PlayFabEditorApi.GetStudios(new PlayFab.Editor.EditorModels.GetStudiosRequest(), (getStudioResult) =>
                    {
                        PlayFabEditorDataService.accountDetails.studios = getStudioResult.Studios.ToList();
                        PlayFabEditorDataService.SaveAccountDetails();
                    }, (getStudiosError) =>
                    {
                        //TODO: Error Handling & have this update when the tab is opened.
                        Debug.LogError(getStudiosError.ToString());
                    });
                }
            GUILayout.EndHorizontal(); 

            foreach(var studio in studioFoldOutStates)
            {
                var style = new GUIStyle(EditorStyles.foldout);

                if(studio.Value.isCollapsed)
                {
                    style.fontStyle = FontStyle.Normal;
                }

                studio.Value.isCollapsed = EditorGUI.Foldout(EditorGUILayout.GetControlRect(), studio.Value.isCollapsed, string.Format("{0} ({1})", studio.Value.Studio.Name, studio.Value.Studio.Titles.Length), true, style);

                if(!studio.Value.isCollapsed)
                {
                    EditorGUI.indentLevel = 1;

                    GUILayout.BeginHorizontal(PlayFabEditorHelper.uiStyle.GetStyle("gpStyleClear"));
                        EditorGUILayout.LabelField("TITLES:", PlayFabEditorHelper.uiStyle.GetStyle("labelStyle"), GUILayout.Width(labelWidth));
                    GUILayout.EndHorizontal();

                    // draw title foldouts
                    foreach(var title in studio.Value.titleFoldOutStates)
                    {
                        title.Value.isCollapsed = EditorGUILayout.Foldout(title.Value.isCollapsed, string.Format("{0} [{1}]", title.Value.Title.Name, title.Value.Title.Id));
                       
                        if(! title.Value.isCollapsed)
                        {
                            EditorGUI.indentLevel = 2;
                            GUILayout.BeginHorizontal(PlayFabEditorHelper.uiStyle.GetStyle("gpStyleClear"));
                                EditorGUILayout.LabelField("SECRET KEY:", PlayFabEditorHelper.uiStyle.GetStyle("labelStyle"), GUILayout.Width(labelWidth));
                                EditorGUILayout.TextField(""+title.Value.Title.SecretKey);
                            GUILayout.EndHorizontal();   

                            GUILayout.BeginHorizontal(PlayFabEditorHelper.uiStyle.GetStyle("gpStyleClear"));
                                EditorGUILayout.LabelField("URL:", PlayFabEditorHelper.uiStyle.GetStyle("labelStyle"), GUILayout.Width(labelWidth));
                                GUILayout.FlexibleSpace();
                                if(GUILayout.Button("VIEW IN GAME MANAGER", PlayFabEditorHelper.uiStyle.GetStyle("textButton")))
                                {
                                    Application.OpenURL(title.Value.Title.GameManagerUrl);
                                }
                                GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();  
                            EditorGUI.indentLevel = 1;
                        }
                    }

                    EditorGUI.indentLevel = 0;
                }
            }
            GUILayout.EndScrollView();
        }


        public static void DrawStandardSettingsSubPanel()
        {
            float labelWidth = 160;

            if(_selectedTitleIdIndex != _prevSelectedTitleIdIndex)
            {
                // this changed since the last loop
                _prevSelectedTitleIdIndex = _selectedStudioIndex;
                _TitleId = titleOptions[_selectedTitleIdIndex]; 



             #if ENABLE_PLAYFABADMIN_API || ENABLE_PLAYFABSERVER_API
                _DeveloperSecretKey = PlayFabEditorDataService.accountDetails.studios[_selectedStudioIndex].Titles[_selectedTitleIdIndex].SecretKey;
             #endif

            }

            if(_selectedStudioIndex != _prevSelectedStudioIndex)
            {
                // this changed since the last loop
                _selectedTitleIdIndex = 0; // reset our titles index

                titleOptions = new string[PlayFabEditorDataService.accountDetails.studios[_selectedStudioIndex].Titles.Length];
                for(var z = 0; z < PlayFabEditorDataService.accountDetails.studios[_selectedStudioIndex].Titles.Length; z++)
                {
                    titleOptions[z] = PlayFabEditorDataService.accountDetails.studios[_selectedStudioIndex].Titles[z].Id;
                }

                _prevSelectedStudioIndex = _selectedStudioIndex;
            }




            GUILayout.BeginVertical(PlayFabEditorHelper.uiStyle.GetStyle("gpStyleGray1"), GUILayout.ExpandWidth(true));

            if(studioOptions != null && studioOptions.Length > 0)
            {
                GUILayout.BeginHorizontal(PlayFabEditorHelper.uiStyle.GetStyle("gpStyleClear"));
                    EditorGUILayout.LabelField("STUDIO: ", PlayFabEditorHelper.uiStyle.GetStyle("labelStyle"), GUILayout.Width(labelWidth));
                    _selectedStudioIndex = EditorGUILayout.Popup(_selectedStudioIndex, studioOptions, PlayFabEditorHelper.uiStyle.GetStyle("TextField"), GUILayout.MinHeight(25));
                GUILayout.EndHorizontal();
            }

            if(titleOptions != null && titleOptions.Length > 0)
            {
                GUILayout.BeginHorizontal(PlayFabEditorHelper.uiStyle.GetStyle("gpStyleClear"));
                    EditorGUILayout.LabelField("TITLE ID: ", PlayFabEditorHelper.uiStyle.GetStyle("labelStyle"), GUILayout.Width(labelWidth));
                    _selectedTitleIdIndex = EditorGUILayout.Popup(_selectedTitleIdIndex, titleOptions, PlayFabEditorHelper.uiStyle.GetStyle("TextField"), GUILayout.MinHeight(25));
                GUILayout.EndHorizontal();
            }

            #if ENABLE_PLAYFABADMIN_API || ENABLE_PLAYFABSERVER_API
                GUILayout.BeginHorizontal(PlayFabEditorHelper.uiStyle.GetStyle("gpStyleClear"));
                    EditorGUILayout.LabelField("DEVELOPER SECRET KEY: ", PlayFabEditorHelper.uiStyle.GetStyle("labelStyle"), GUILayout.Width(labelWidth));
                    _DeveloperSecretKey = EditorGUILayout.TextField(_DeveloperSecretKey, PlayFabEditorHelper.uiStyle.GetStyle("TextField"), GUILayout.MinHeight(25));
                GUILayout.EndHorizontal();

            #endif

            GUILayout.BeginHorizontal(PlayFabEditorHelper.uiStyle.GetStyle("gpStyleClear"));
                EditorGUILayout.LabelField("REQUEST TYPE: ", PlayFabEditorHelper.uiStyle.GetStyle("labelStyle"), GUILayout.MaxWidth(labelWidth));
                _RequestType = (WebRequestType) EditorGUILayout.EnumPopup(_RequestType, PlayFabEditorHelper.uiStyle.GetStyle("TextField"), GUILayout.Height(25));
            GUILayout.EndHorizontal();


            if (_RequestType == WebRequestType.HttpWebRequest)
            {
                using (FixedWidthLabel fwl = new FixedWidthLabel(new GUIContent("REQUEST TIMEOUT: "), PlayFabEditorHelper.uiStyle.GetStyle("labelStyle")))
                {
                    GUILayout.Space(labelWidth - fwl.fieldWidth);
                    _RequestTimeOut = EditorGUILayout.IntField(_RequestTimeOut, PlayFabEditorHelper.uiStyle.GetStyle("TextField"), GUILayout.MinHeight(25));
                }

                using (FixedWidthLabel fwl = new FixedWidthLabel(new GUIContent("KEEP ALIVE: "), PlayFabEditorHelper.uiStyle.GetStyle("labelStyle")))
                {
                    GUILayout.Space(labelWidth - fwl.fieldWidth);
                    _KeepAlive = EditorGUILayout.Toggle(_KeepAlive, PlayFabEditorHelper.uiStyle.GetStyle("Toggle"), GUILayout.MinHeight(25));
                }
            }


            GUILayout.BeginHorizontal(PlayFabEditorHelper.uiStyle.GetStyle("gpStyleClear"));
                EditorGUILayout.LabelField("COMPRESS API DATA: ", PlayFabEditorHelper.uiStyle.GetStyle("labelStyle"), GUILayout.MaxWidth(labelWidth));
                _CompressApiData = EditorGUILayout.Toggle(_CompressApiData, PlayFabEditorHelper.uiStyle.GetStyle("Toggle"), GUILayout.MinHeight(25));
            GUILayout.EndHorizontal();
           

            GUILayout.BeginHorizontal(PlayFabEditorHelper.uiStyle.GetStyle("gpStyleClear"));
                var buttonWidth = 100;
                GUILayout.Space(EditorGUIUtility.currentViewWidth - buttonWidth);

                if (GUILayout.Button("SAVE", PlayFabEditorHelper.uiStyle.GetStyle("Button"), GUILayout.MinHeight(32), GUILayout.MaxWidth(buttonWidth)))
                {
                    OnSaveSettings();
                }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        #endregion


        #region unity-like loops

        public static void Update()
        {
            buildTargets = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';').ToList();
            if (EditorPrefs.HasKey(AdminAPI))
            {
                _isAdminSdkEnabled = true;
            }
            if (EditorPrefs.HasKey(ServerAPI))
            {
                _isServerSdkEnabled = true;
            }
            if (EditorPrefs.HasKey(DebugRequestTiming))
            {
                _IsDebugRequestTiming = true;
            }
            if (EditorPrefs.HasKey(ClientAPI))
            {
                _isClientSdkEnabled = false;
            }
        }

        public static void After()
        {
            if (PlayFabEditorDataService.envDetails.isAdminApiEnabled && !buildTargets.Contains(AdminAPI))
            {
                var str = AddToBuildTarget(buildTargets, AdminAPI);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, str);
                //EditorPrefs.SetString(AdminAPI, "1");
                //PlayFabEditorDataService.envDetails.isAdminApiEnabled = true;
                PlayFabEditorDataService.SaveEnvDetails();
            }
            else if (!PlayFabEditorDataService.envDetails.isAdminApiEnabled && buildTargets.Contains(AdminAPI))
            {
                var str = RemoveToBuildTarget(buildTargets, AdminAPI);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, str);
                //EditorPrefs.DeleteKey(AdminAPI);
                //PlayFabEditorDataService.envDetails.isAdminApiEnabled = false;
                PlayFabEditorDataService.SaveEnvDetails();
            }

            if (PlayFabEditorDataService.envDetails.isServerApiEnabled  && !buildTargets.Contains(ServerAPI))
            {
                var str = AddToBuildTarget(buildTargets, ServerAPI);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, str);
                //EditorPrefs.SetString(ServerAPI, "1");
               // PlayFabEditorDataService.envDetails.isServerApiEnabled = true;
                PlayFabEditorDataService.SaveEnvDetails();
            }
            else if (!PlayFabEditorDataService.envDetails.isServerApiEnabled  && buildTargets.Contains(ServerAPI))
            {
                var str = RemoveToBuildTarget(buildTargets, ServerAPI);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, str);
                //EditorPrefs.DeleteKey(ServerAPI);
                //PlayFabEditorDataService.envDetails.isServerApiEnabled = false;
                PlayFabEditorDataService.SaveEnvDetails();
            }

            if (PlayFabEditorDataService.envDetails.isDebugRequestTimesEnabled && !buildTargets.Contains(DebugRequestTiming))
            {
                var str = AddToBuildTarget(buildTargets, DebugRequestTiming);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, str);
                //EditorPrefs.SetString(DebugRequestTiming, "1");
                //PlayFabEditorDataService.envDetails.isDebugRequestTimesEnabled = true;
                PlayFabEditorDataService.SaveEnvDetails();
            }
            else if (!PlayFabEditorDataService.envDetails.isDebugRequestTimesEnabled && buildTargets.Contains(DebugRequestTiming))
            {
                var str = RemoveToBuildTarget(buildTargets, DebugRequestTiming);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, str);
                //EditorPrefs.DeleteKey(DebugRequestTiming);
                //PlayFabEditorDataService.envDetails.isDebugRequestTimesEnabled = false;
                PlayFabEditorDataService.SaveEnvDetails();
            }

            if (!PlayFabEditorDataService.envDetails.isClientApiEnabled && !buildTargets.Contains(ClientAPI))
            {
                Debug.Log(ClientAPI + ":" + buildTargets.Contains(ClientAPI));
                var str = AddToBuildTarget(buildTargets, ClientAPI);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, str);
                //EditorPrefs.SetString(ClientAPI, "1");
                //PlayFabEditorDataService.envDetails.isClientApiEnabled = true;
                PlayFabEditorDataService.SaveEnvDetails();
            }
            else if (PlayFabEditorDataService.envDetails.isClientApiEnabled && buildTargets.Contains(ClientAPI))
            {
                Debug.Log(ClientAPI + "- Removed");
                var str = RemoveToBuildTarget(buildTargets, ClientAPI);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, str);
                //EditorPrefs.DeleteKey(ClientAPI);
                //PlayFabEditorDataService.envDetails.isClientApiEnabled = false;
                PlayFabEditorDataService.SaveEnvDetails();
            }

        }

        #endregion


        #region menu and helper methods
        public static void SetSettingsProperties()
        {
            if (PlayFabEditorSDKTools.IsInstalled && !_isSettingsSet)
            {
                if(studioOptions == null || studioOptions.Length == 0 && PlayFabEditorDataService.accountDetails.studios.Count > 0)
                {
                    studioOptions = new string[PlayFabEditorDataService.accountDetails.studios.Count];
                    //studioOptions[0] = string.Empty;

                   
                    for(var z = 0; z < PlayFabEditorDataService.accountDetails.studios.Count; z++)
                    {
                        //note the 1 index
                        studioOptions[z] = PlayFabEditorDataService.accountDetails.studios[z].Name;

                        if(PlayFabEditorDataService.accountDetails.studios[z].Name == PlayFabEditorDataService.envDetails.selectedStudio)
                        {
                            _prevSelectedStudioIndex = z;
                            _selectedStudioIndex = z;
                        }

                        bool foundTitle = false;
                        for(var x = 0; x < PlayFabEditorDataService.accountDetails.studios[z].Titles.Length; x++)
                        {   
                            if(foundTitle) 
                            {
                                // then we know this is the correct studio
                                titleOptions[x] = PlayFabEditorDataService.accountDetails.studios[z].Titles[x].Id;
                            }

                            
                            if(PlayFabEditorDataService.accountDetails.studios[z].Titles[x].Id == PlayFabEditorDataService.envDetails.selectedTitleId && foundTitle == false)
                            {   
                                foundTitle = true;
                                titleOptions = new string[PlayFabEditorDataService.accountDetails.studios[z].Titles.Length];

                                #if ENABLE_PLAYFABADMIN_API || ENABLE_PLAYFABSERVER_API
                                    _DeveloperSecretKey =  PlayFabEditorDataService.accountDetails.studios[z].Titles[x].SecretKey;
                                #endif

                                _selectedTitleIdIndex = x;
                                _prevSelectedTitleIdIndex = x;

                                _selectedStudioIndex = z;
                                _prevSelectedStudioIndex = z;

                                // restart this inner loop to add titles to dropdown
                                x = 0;
                            }
                        }

                    }

                    //TODO build a fallback for this case. Currently this may cause unexpected behaviors on the settings screen
                    // in these cases, we did not find the title or studio that we were looking for
                    //if editorprefs studio is null & SO.titleId &| devKey is set, then we are likly using a title that we are not a member of.
                    if(_selectedStudioIndex == -1)
                    {
                        
                    }

                    if(_selectedTitleIdIndex == -1)
                    {

                    }
                }

                _RequestTimeOut = PlayFabEditorDataService.envDetails.timeOut;
                _KeepAlive = PlayFabEditorDataService.envDetails.keepAlive;

                _isSettingsSet = true;

            }
            
        }

        private static void OnApiSettingsClicked()
        {
            EditorPrefs.SetInt("PLAYFAB_CURRENT_SETTINGSMENU", (int)SubMenuStates.ApiSettings);
        }

        private static void OnStandardSetttingsClicked()
        {
            EditorPrefs.SetInt("PLAYFAB_CURRENT_SETTINGSMENU", (int)SubMenuStates.StandardSettings);
        }

        private static void OnTitleSettingsClicked()
        {
            EditorPrefs.SetInt("PLAYFAB_CURRENT_SETTINGSMENU", (int)SubMenuStates.TitleSettings);
        }


        private static void OnSaveSettings()
        {
            if (PlayFabEditorSDKTools.IsInstalled && PlayFabEditorSDKTools.isSdkSupported)
            {
                PlayFabEditorDataService.envDetails.titleData.Clear();

                PlayFabEditorDataService.envDetails.selectedStudio = studioOptions[_selectedStudioIndex];  
                PlayFabEditorDataService.envDetails.selectedTitleId = titleOptions[_selectedTitleIdIndex];

                #if ENABLE_PLAYFABADMIN_API || ENABLE_PLAYFABSERVER_API
                    PlayFabEditorDataService.envDetails.developerSecretKey = _DeveloperSecretKey;
                #endif
     
                PlayFabEditorDataService.envDetails.compressApiData = _CompressApiData;
                PlayFabEditorDataService.envDetails.keepAlive = _KeepAlive;
                PlayFabEditorDataService.envDetails.webRequestType = _RequestType;
                PlayFabEditorDataService.envDetails.timeOut = _RequestTimeOut;

                PlayFabEditorDataService.SaveEnvDetails();

            }
            else
            {
                PlayFabEditor.RaiseStateUpdate(PlayFabEditor.EdExStates.OnError, "SDK is unsupported or not installed");
            }
        }


        public static string AddToBuildTarget(List<string> targets, string define)
        {
            targets.Add(define);
            return string.Join(";", targets.ToArray());
        }

        public static string RemoveToBuildTarget(List<string> targets, string define)
        {
            targets.Remove(define);
            return string.Join(";", targets.ToArray());
        }

        //CTOR
        static PlayFabEditorSettings()
        {
            if(!PlayFabEditor.IsEventHandlerRegistered(StateUpdateHandler))
            {
                PlayFabEditor.EdExStateUpdate += StateUpdateHandler;
            }
        }

        /// <summary>
        /// Handles state updates within the editor extension.
        /// </summary>
        /// <param name="state">the state that triggered this event.</param>
        /// <param name="status">a generic message about the status.</param>
        /// <param name="json">a generic container for additional JSON encoded info.</param>
        public static void StateUpdateHandler(PlayFabEditor.EdExStates state, string status, string json)
        {
            //Debug.Log(string.Format("PFE: Handled EdExStatusUpdate:{0}, Status:{1}, Misc:{2}", state, status, json)); 



            switch(state)
            {
//                case EdExStates.OnEnable:
//                   
//                break;
//                case EdExStates.OnDisable:
//                   
//                break;
//                case EdExStates.OnLogin:
//                   
//                break;
//                case EdExStates.OnLogout:
//                   
//                break;
                case PlayFabEditor.EdExStates.OnMenuItemClicked:
                    if(status == "Settings")
                    {
                        EditorPrefs.SetInt("PLAYFAB_CURRENT_SETTINGSMENU", (int)SubMenuStates.StandardSettings);
                        _subMenuState = SubMenuStates.StandardSettings;
                    }
                break;
//
//                case EdExStates.OnSubmenuItemClicked:
//                   
//                break;
//
            }
        }
        #endregion
    }


    public class StudioDisplaySet
    {
        public PlayFab.Editor.EditorModels.Studio Studio;
        public bool isCollapsed = true;
        public Dictionary<string, TitleDisplaySet> titleFoldOutStates = new Dictionary<string, TitleDisplaySet>();
    }

    public class TitleDisplaySet
    {
        public PlayFab.Editor.EditorModels.Title Title;
        public bool isCollapsed = true;
    }


}
