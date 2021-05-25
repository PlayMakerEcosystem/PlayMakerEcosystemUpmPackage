//-----------------------------------------------------------------------
// <copyright file="EcosystemPackager.cs" company="Hutong Games LLC">
// Copyright (c) Hutong Games LLC. All rights reserved.
// </copyright>
// <author name="Jean Fabre"</author>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MyUtils = HutongGames.PlayMakerEditor.Addons.Ecosystem.Utils; // conflict with Unity Remote utils public class... odd
using System.IO;

using System.Text.RegularExpressions;

namespace HutongGames.PlayMakerEditor.Addons.Ecosystem.Publishing
{
    public class EcosystemCustomActionPackager : EditorWindow
    {
        [MenuItem("PlayMaker/Addons/Ecosystem/Ecosystem Custom Action Packager", false, 1000)]
        static void Init()
        {
            //Debug.Log("################ Init");


            // Get existing open window or if none, make a new one:
            EcosystemCustomActionPackager Instance =
                (EcosystemCustomActionPackager) EditorWindow.GetWindow(typeof(EcosystemCustomActionPackager));

            Instance.position = new Rect(100, 100, 200, 200);
            Instance.minSize = new Vector2(200,200);

            string _ecosystemSkinPath = "";
            GUISkin _ecosystemSkin = MyUtils.GetGuiSkin("PlayMakerEcosystemGuiSkin", out _ecosystemSkinPath);

            Texture _iconTexture =
                _ecosystemSkin.FindStyle("Ecosystem Logo Embossed @12px").normal.background as Texture;

            Instance.titleContent = new GUIContent("Ecosystem", _iconTexture, "Custom Action Packager");
        }

        private bool _processing;
        private string ActionBeingProcessed;

        private bool Cancel;

        private List<string> AllCustomActions;

        private bool _ProcessJustMetaFiles;

        private Dictionary<string, string> ActionDescriptions;
        void OnGUI()
        {
            wantsMouseMove = true;
            if (Event.current.type == EventType.MouseMove) Repaint ();
            
          //  GUILayout.Label("Processing <" + _processing + ">");
          //  GUILayout.Label("cancel <" + Cancel + ">");

            if (!_processing)
            {
                _ProcessJustMetaFiles = GUILayout.Toggle(_ProcessJustMetaFiles, "Process just meta files");
                if (GUILayout.Button("Package All Custom Actions"))
                {
                    OnExportallActionsButtonClicked();
                }

                

            }
            else
            {
                if (GUILayout.Button("Cancel"))
                {
                    _processing = false;
                    Cancel = true;
                }

                GUILayout.Label(ActionBeingProcessed);
            }
        }

        void OnExportallActionsButtonClicked()
        {
            EditorCoroutine.start(
                ProceedWithAllActionsExport()
            );
        }

        IEnumerator ProceedWithAllActionsExport()
        {
            _processing = true;
            Debug.Log("ProceedWithAllActionsExport");
            
            string _projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);

            ActionDescriptions  = new Dictionary<string, string>();
            
            // load description json file
            string JsonDocDescriptionFilePath = _projectPath + "PlayMaker/Json/actions.json";
            if (File.Exists(JsonDocDescriptionFilePath))
            {
                string _rawJsonDocDescription = File.ReadAllText(JsonDocDescriptionFilePath);
           
                ArrayList _jsonParsed = (ArrayList)JSON.JsonDecode(_rawJsonDocDescription);
                if (_jsonParsed != null)
                {
                    foreach (Hashtable _actionJson in _jsonParsed)
                    {
                        if (_actionJson.ContainsKey("description"))
                        {
                         
                       string _name = (string) _actionJson["name"];
                 
                       _name = _name.Replace(" ", "");
                       
                     //  Debug.Log("we have a description for " + _name);
                            ActionDescriptions[_name] = (string) _actionJson["description"];
                        }
                    }
                }
                else
                {
                    _processing = false;
                    Debug.LogError("woupsss");
                    yield break;
                }
            }
   
                
            AllCustomActions = new List<string>();

            string[] assetPaths = AssetDatabase.GetAllAssetPaths();
            foreach (string assetPath in assetPaths)
            {
                if (assetPath.StartsWith("Assets") && assetPath.EndsWith(".cs"))
                {
                    AllCustomActions.Add(Application.dataPath + assetPath.Substring(6));
                }
            }
            
         
            string AssetActionPath = "PlayMaker/Ecosystem/PlayMaker Custom Actions";
            string _customActionsPath = _projectPath + AssetActionPath;
            string _actionText = String.Empty;
            
            UnityEngine.Object scriptAsset;
            if (ActionScripts.ActionScriptLookup.Count == 0)
            {
                Actions.BuildList();
            }

            List<string> includeFileList;
            
            foreach (Type actionType in Actions.List)
            {
                includeFileList = new List<string>();
                
                string str3 = Labels.StripNamespace(actionType.ToString());
                string actionLabel = Labels.GetActionLabel(actionType);
                string category = Actions.GetCategory(actionType);
                ActionBeingProcessed = category + "/" + actionLabel + "  " + str3;
                
                ActionScripts.ActionScriptLookup.TryGetValue(actionType, out scriptAsset);

                string _assetPath = AssetDatabase.GetAssetPath(scriptAsset);
                
              //  Debug.Log(ActionBeingProcessed + "\n" + _assetPath);

                if (!string.IsNullOrEmpty(_assetPath))
                {
                    _actionText =  File.ReadAllText(_assetPath);
                    if (_actionText.Contains("__ECO__") &&
                        _actionText.Contains("__PLAYMAKER__") &&
                        _actionText.Contains("__ACTION__"))
                    {
                        
                        bool _export = _ProcessJustMetaFiles;
                        
                        // check if the package exists
                        string _packagePath = _customActionsPath + "/" + category + "/" + str3 + ".unitypackage";
                        FileInfo _packagefile = new FileInfo(_packagePath);
                        FileInfo _actionFile = new FileInfo(_assetPath);
                        
                        if (File.Exists(_packagePath))
                        {
                            // if exists, if file is newer than package date, will export
                          // Debug.Log(_actionFile.LastWriteTime + " "+ _packagefile.LastWriteTime);
                            if (_actionFile.LastWriteTime > _packagefile.LastWriteTime)
                            {
                                _export = true;
                            }
                        }
                        else
                        {
                            Directory.CreateDirectory(_packagefile.Directory.ToString());
                            _export = true;
                        }
                       
                        // need to also take into acount dependencies and if they are newer.

                        Hashtable _metaData = EcosystemUtils.ExtractEcoMetaDataFromText(_actionText);

                        if  (_metaData.ContainsKey("script dependancies"))
                        {
                          if (_export)  Debug.Log("We have dependancies");
                            ArrayList _dependancies = (ArrayList)_metaData["script dependancies"];
                            if (_dependancies!=null)
                            {
                                foreach(object dScript in _dependancies)
                                {
                                    
                                    string _dscript = (string)dScript;
                                    if (_export)  Debug.Log("dependancy: "+_dscript);
                                    
                                    if (_dscript.StartsWith("Assets/"))
                                    {
                                        includeFileList.Add(_dscript);
                                        
                                        // check for export if dependency newer than package
                                        if (new FileInfo(Application.dataPath + _dscript.Substring(6)).LastWriteTime > _packagefile.LastWriteTime)
                                        {
                                            _export = true;
                                        }
                                    }
                                }
                            }
                        }
                        

                        if (_export)
                        {
                            Debug.Log("Action: "+category+"/"+str3+ ": proceed with exporting " );

                            string _prekeywords = "";
                            // extract all keywords
                            string pattern = @"(\w+\b)(?!.*\1\b)";
                            Regex rgx = new Regex(pattern,RegexOptions.IgnoreCase);
                            int _intMatchTest;
                            float _floatMatchTest;
                            foreach (Match match in rgx.Matches(_actionText))
                            {
                                if (match.Value.Length <= 2) continue;
                                if (int.TryParse(match.Value,out _intMatchTest)) continue;
                                if (float.TryParse(match.Value,out _floatMatchTest)) continue;
                                if (match.Value.Contains("UNITY")) continue;
                                if (
                                    ExcludedKeywords.IndexOf(match.Value, 0, StringComparison.InvariantCultureIgnoreCase) != -1
                                    ) continue;
                            
                                
                                _prekeywords += " " + match.Value;
                            }

                            string _keywords = "";
                            foreach (Match match in rgx.Matches(_prekeywords))
                            {
                                _keywords += " " + match.Value;
                            }
                            
                            includeFileList.Add(_assetPath);
                              
                            foreach (string s in includeFileList)
                            {
                                Debug.Log(s);
                            }
                            
                            if (!_ProcessJustMetaFiles)
                            {
                                AssetDatabase.ExportPackage(includeFileList.ToArray(), _packagePath, ExportPackageOptions.Default);
                            }

                            string _packageDataPath = AssetActionPath + "/" + category + "/" + str3 + ".unitypackage";
                            
                            BuildPackageMetaFile(str3, category,_keywords, _assetPath, _packagePath,_packageDataPath);
                            
                            Debug.Log("Action: "+category+"/"+str3+ ": Done");
                        }
                        
                    }
                    
                }
                Repaint ();
                yield return new WaitForSeconds(2f);

                if (Cancel)
                {
                    _processing = false;
                    Cancel = false;
                    yield break;
                }
            }
            
            Debug.Log("We are done");
            
            _processing = false;
            Cancel = false;
            
            Repaint ();
        }

        /**
          {
                "__ECO__":"__PACKAGE__",
                "Author":"Jean Fabre",
                "Type":"Action",
                "Version":"1.4",
                "UnityMinimumVersion":"2018",
                "PlayMakerMinimumVersion":"1.9",
                "unitypackage":"/PlayMaker/Ecosystem/Custom Packages/ArrayMaker/ArrayMaker.unitypackage",
                "pingAssetPath":"Assets/PlayMaker Custom actions/Catogory/action.cs",
                "YoutubeVideos":["https://www.youtube.com/watch?v=6SBuH1vxC7A&list=PL1EwQda_5HM_n8MEaBGfcXLNpmL8qBqV1"],
                "WebLink":"https://hutonggames.fogbugz.com/?W715",
                "keywords":"List Array Hashtable dictionnary data database"
            }
         **/

        void BuildPackageMetaFile(string actionName,string category,string keywords, string assetPath,string packagePath,string packageDataPath)
        {

            string PackageTextPath = packagePath;
            PackageTextPath = PackageTextPath.Replace(".unitypackage", ".action.txt");

            string _packageText = "{\n";
            _packageText += "\"__ECO__\":\"__PACKAGE__\",\n";
            _packageText += "\"Type\":\"Action\",\n";
            _packageText += "\"unitypackage\":\""+packageDataPath+"\",\n";
            _packageText += "\"pingAssetPath\":\""+assetPath+"\",\n";
            _packageText += "\"keywords\":\""+keywords+"\"";
            if (ActionDescriptions != null && ActionDescriptions.ContainsKey(actionName))
            {
                _packageText += ",\n";
                _packageText += "\"documentation\":{\n";
                _packageText += "\"description\":\""+ActionDescriptions[actionName]+"\"\n}\n";
            }
            else
            {
                _packageText += "\n"; 
            }
            
            _packageText += "}";
            bool jsonOk = false;
            JSON.JsonDecode(_packageText, ref jsonOk);
            if (jsonOk)
            {
                FileInfo _fl = new FileInfo(PackageTextPath);
                Directory.CreateDirectory(_fl.Directory.ToString());
                Debug.Log("creating package text in"+PackageTextPath);
                File.WriteAllText(PackageTextPath, _packageText);
                
                Debug.Log("created package text for "+category+"/"+actionName+" in"+PackageTextPath+"\n"+_packageText);
            }
            else
            {
                Debug.LogError("Failed to create package text file");
            }
            
        }

        private static string ExcludedKeywords =
            "abstract as base break case catch char checked class const continue decimal" +
            " default delegate do double else explicit extern false finally fixed" +
            " for foreach goto if implicit in interface internal is lock long namespace new " +
            " object operator out override params private protected public readonly ref return sbyte" +
            " sealed short sizeof stackalloc static struct switch this throw true try typeof" +
            " uint ulong unchecked unsafe ushort using virtual void volatile while" +
            " HutongGames LLC Copyrights Attribution International reserved" +
            "__ECO__ __PLAYMAKER__ __ACTION__ EcoMetaStart EcoMetaEnd Keywords License" +
            "ActionCategory Tooltip CompoundArray RequiredField ActionSection HelpUrl UIHint UseVariable Finish HasFloatSlider ActionTarget" +
            "FsmStateAction FsmOwnerDefault  FsmColor  FsmVar FsmEvent" +
            "OnEnter OnUpdate OnFixedUpdate OnLateUpdate OnActionUpdate useFixedUpdate HandleFixedUpdate OnPreprocess HandleLateUpdate OwnerDefaultOption UseOwner GetComponent Missing OwnerDefaultOption" +
            "VariableType CheckForComponent GetOwnerDefaultTarget IsNone IsNullOrEmpty Debug Log LogError LogWarning " +
            "playmakerforum LogError" +
            "UnityEngine Color32 com from Editor Collections System";
    }
}