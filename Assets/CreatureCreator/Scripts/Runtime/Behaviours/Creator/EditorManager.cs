﻿// Creature Creator - https://github.com/daniellochner/Creature-Creator
// Copyright (c) Daniel Lochner

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using RotaryHeart.Lib.SerializableDictionary;
using UnityFBXExporter;
using ProfanityDetector;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;
using Crosstales.FB;

#if UNITY_STANDALONE
using Steamworks;
#endif

namespace DanielLochner.Assets.CreatureCreator
{
    public class EditorManager : MonoBehaviourSingleton<EditorManager>
    {
        #region Fields
        [SerializeField] private bool creativeMode;
        [SerializeField] private bool checkForProfanity;
        [SerializeField] private int creativeCash;
        [SerializeField] private SecretKey creatureEncryptionKey;

        [Header("General")]
        [SerializeField] private AudioSource editorAudioSource;
        [SerializeField] private CanvasGroup editorCanvasGroup;
        [SerializeField] private CanvasGroup paginationCanvasGroup;
        [SerializeField] private int historyLimit;

        [Header("Build")]
        [SerializeField] private Menu buildMenu;
        [SerializeField] private ToggleGraphic buildTG;
        [SerializeField] private BodyPartSettingsMenu bodyPartSettingsMenu;
        [SerializeField] private BodyPartUI bodyPartUIPrefab;
        [SerializeField] private TextMeshProUGUI cashText;
        [SerializeField] private TextMeshProUGUI complexityText;
        [SerializeField] private TextMeshProUGUI heightText;
        [SerializeField] private TextMeshProUGUI weightText;
        [SerializeField] private TextMeshProUGUI dietText;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private TextMeshProUGUI bonesText;
        [SerializeField] private TextMeshProUGUI bodyPartsText;
        [SerializeField] private TextMeshProUGUI abilitiesText;
        [SerializeField] private Toggle bodyPartsToggle;
        [SerializeField] private Toggle abilitiesToggle;
        [SerializeField] private Animator cashWarningAnimator;
        [SerializeField] private Animator complexityWarningAnimator;
        [SerializeField] private Animator bonesWarningAnimator;
        [SerializeField] private AudioClip whooshAudioClip;
        [SerializeField] private AudioClip errorAudioClip;
        [SerializeField] private AudioClip createAudioClip;
        [SerializeField] private RectTransform bodyPartsRT;
        [SerializeField] private BodyPartGrids bodyPartGrids;
        [SerializeField] private GameObject noPartsText;
        [SerializeField] private CanvasGroup undoBuildCG;
        [SerializeField] private CanvasGroup redoBuildCG;

        [Header("Play")]
        [SerializeField] private Menu playMenu;
        [SerializeField] private ToggleGraphic playTG;
        [SerializeField] private CreatureInformationMenu informationMenu;

        [Header("Paint")]
        [SerializeField] private Menu paintMenu;
        [SerializeField] private ToggleGraphic paintTG;
        [SerializeField] private PatternSettingsMenu patternSettingsMenu;
        [SerializeField] private ColourSettingsMenu colourSettingsMenu;
        [SerializeField] private PatternUI patternUIPrefab;
        [SerializeField] private Material patternMaterial;
        [SerializeField] private ColourPalette primaryColourPalette;
        [SerializeField] private ColourPalette secondaryColourPalette;
        [SerializeField] private GameObject primaryColourOverride;
        [SerializeField] private GameObject secondaryColourOverride;
        [SerializeField] private ToggleGroup patternsToggleGroup;
        [SerializeField] private RectTransform patternsRT;
        [SerializeField] private TextMeshProUGUI noColoursText;
        [SerializeField] private GameObject noPatternsText;
        [SerializeField] private CanvasGroup undoPaintCG;
        [SerializeField] private CanvasGroup redoPaintCG;

        [Header("Options")]
        [SerializeField] private SimpleSideMenu optionsSideMenu;
        [SerializeField] private CanvasGroup optionsCanvasGroup;
        [SerializeField] private CreatureUI creatureUIPrefab;
        [SerializeField] private TMP_InputField creatureNameText;
        [SerializeField] private ToggleGroup creaturesToggleGroup;
        [SerializeField] private RectTransform creaturesRT;
        [SerializeField] private GameObject noCreaturesText;

        [Header("Testing")]
        [SerializeField, Button("UnlockRandomBodyPart")] private bool unlockRandomBodyPart;
        [SerializeField, Button("UnlockRandomPattern")] private bool unlockRandomPattern;

        private List<CreatureUI> creaturesUI = new List<CreatureUI>();
        private List<PatternUI> patternsUI = new List<PatternUI>();
        private string creaturesDirectory = null; // null because Application.persistentDataPath cannot be called during serialization.
        private bool isVisible = true, isEditing = true;
        private Coroutine visibleCoroutine;
        private CreatureUI currentCreatureUI;

        private Coroutine delayedRecordCoroutine;
        private Change prevDelayedChangeType;

#if UNITY_STANDALONE
        private WorkshopData currentWorkshopData;
        private UGCUpdateHandle_t handle;
#endif
        #endregion

        #region Properties
        public bool CheckForProfanity
        {
            get => checkForProfanity;
            set
            {
                checkForProfanity = value;
                Creature.Messenger.CheckForProfanity = checkForProfanity;
            }
        }
        public bool CreativeMode
        {
            get => creativeMode;
            set => creativeMode = value;
        }
        public int CreativeCash
        {
            get => creativeCash;
            set => creativeCash = value;
        }

        public bool IsEditing => isEditing;
        public bool IsBuilding => buildMenu.IsOpen;
        public bool IsPainting => paintMenu.IsOpen;
        public bool IsPlaying => playMenu.IsOpen;

        public CreaturePlayerLocal Creature => Player.Instance;

        public int BaseCash
        {
            get => CreativeMode ? CreativeCash : ProgressManager.Data.Cash;
        }
        public bool Unlimited
        {
            get
            {
                if (WorldManager.Instance.World is WorldSP)
                {
                    return (WorldManager.Instance.World as WorldSP).Unlimited;
                }
                return false;
            }
        }

        public List<ChangeData> History { get; set; } = new List<ChangeData>();
        public int Counter { get; set; } = -1;
        #endregion

        #region Methods
        private void Update()
        {
            if (SystemUtility.IsDevice(DeviceType.Desktop))
            {
                HandleKeyboardShortcuts();
            }
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        #region Setup
        public void Setup()
        {
            SetupPlayer();
            SetupEditor();

            TakeSnapshot(Change.Load);
        }
        public void SetupEditor()
        {
            // Build
            List<string> unlockedBodyParts = null;
            if (CreativeMode)
            {
                unlockedBodyParts = DatabaseManager.GetDatabase("Body Parts").Objects.Keys.ToList();
            }
            else
            {
                unlockedBodyParts = ProgressManager.Data.UnlockedBodyParts;
            }
            noPartsText.SetActive(unlockedBodyParts.Count == 0);
            foreach (string bodyPartID in unlockedBodyParts)
            {
                if (!SettingsManager.Data.HiddenBodyParts.Contains(bodyPartID)) AddBodyPartUI(bodyPartID);
            }
            UpdateBodyPartTotals();
            bodyPartsToggle.onValueChanged.AddListener(delegate
            {
                // Create a dictionary of distinct body parts with their respective counts.
                Dictionary<string, int> distinctBodyParts = new Dictionary<string, int>();
                foreach (AttachedBodyPart attachedBodyPart in Creature.Constructor.Data.AttachedBodyParts)
                {
                    string bodyPartID = attachedBodyPart.bodyPartID;
                    if (distinctBodyParts.ContainsKey(bodyPartID))
                    {
                        distinctBodyParts[bodyPartID]++;
                    }
                    else
                    {
                        distinctBodyParts.Add(bodyPartID, 1);
                    }
                }

                // Create a list of (joinable) strings using the dictionary of distinct body parts.
                List<string> bodyPartTotals = new List<string>();
                foreach (KeyValuePair<string, int> distinctBodyPart in distinctBodyParts)
                {
                    BodyPart bodyPart = DatabaseManager.GetDatabaseEntry<BodyPart>("Body Parts", distinctBodyPart.Key);
                    int bodyPartCount = distinctBodyPart.Value;

                    if (bodyPartCount > 1)
                    {
                        bodyPartTotals.Add($"{bodyPart} ({bodyPartCount})");
                    }
                    else
                    {
                        bodyPartTotals.Add(bodyPart.ToString());
                    }
                }

                int count = Creature.Constructor.Data.AttachedBodyParts.Count;
                string bodyParts = (bodyPartTotals.Count > 0) ? string.Join(", ", bodyPartTotals) : LocalizationUtility.Localize("cc_build_statistics_none");

                bodyPartsText.SetArguments(bodyPartsToggle.isOn ? bodyParts : count.ToString());
            });
            abilitiesToggle.onValueChanged.AddListener(delegate
            {
                int count = Creature.Abilities.Abilities.Count;
                string abilities = (count > 0) ? string.Join(", ", Creature.Abilities.Abilities) : LocalizationUtility.Localize("cc_build_statistics_none");

                abilitiesText.SetArguments((abilitiesToggle.isOn ? abilities : count.ToString()));
            });

            // Paint
            patternMaterial = new Material(patternMaterial);
            List<string> unlockedPatterns = null;
            if (CreativeMode)
            {
                unlockedPatterns = DatabaseManager.GetDatabase("Patterns").Objects.Keys.ToList();
            }
            else
            {
                unlockedPatterns = ProgressManager.Data.UnlockedPatterns;
            }
            noPatternsText.SetActive(unlockedPatterns.Count == 0);
            foreach (string patternID in unlockedPatterns)
            {
                if (!SettingsManager.Data.HiddenBodyParts.Contains(patternID)) AddPatternUI(patternID);
            }
            primaryColourPalette.ClickUI.OnRightClick.AddListener(delegate
            {
                if (primaryColourOverride.activeSelf)
                {
                    ConfirmationDialog.Confirm(LocalizationUtility.Localize("cc_revert-colour"), LocalizationUtility.Localize("cc_revert-colour-primary"), onYes: delegate
                    {
                        Creature.Editor.PaintedBodyPart.BodyPartConstructor.IsPrimaryOverridden = false;
                        SetPrimaryColourUI(Creature.Constructor.Data.PrimaryColour, false);
                    });
                }
            });
            secondaryColourPalette.ClickUI.OnRightClick.AddListener(delegate
            {
                if (secondaryColourOverride.activeSelf)
                {
                    ConfirmationDialog.Confirm(LocalizationUtility.Localize("cc_revert-colour"), LocalizationUtility.Localize("cc_revert-colour-secondary"), onYes: delegate
                    {
                        Creature.Editor.PaintedBodyPart.BodyPartConstructor.IsSecondaryOverridden = false;
                        SetSecondaryColourUI(Creature.Constructor.Data.SecondaryColour, false);
                    });
                }
            });

            // Options
            creaturesDirectory = Path.Combine(Application.persistentDataPath, "creature");
            if (!Directory.Exists(creaturesDirectory))
            {
                Directory.CreateDirectory(creaturesDirectory);
            }
            foreach (string creaturePath in Directory.GetFiles(creaturesDirectory))
            {
                CreatureData creatureData = SaveUtility.Load<CreatureData>(creaturePath, creatureEncryptionKey.Value);
                if (creatureData != null)
                {
                    CreatureUI creatureUI = AddCreatureUI(Path.GetFileNameWithoutExtension(creaturePath));
                    UpdateLoadableCreatureUI(creatureUI);
                }
            }
            UpdateNoCreatures();

            // Other
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }
        public void SetupPlayer()
        {
            Creature.Setup();

            // Load preset/null creature (and defaults).
            List<CreatureData> presets = SettingsManager.Data.CreaturePresets;
            if (presets.Count > 0)
            {
                Creature.Editor.Load(presets[UnityEngine.Random.Range(0, presets.Count)]);
            }
            else
            {
                Creature.Editor.Load(null);
            }
            Creature.Editor.Cash = BaseCash;
            Creature.Informer.Setup(informationMenu);
            Creature.Mover.Teleport(Creature.Editor.Platform, true);

            Creature.Editor.OnTryRemoveBone += delegate
            {
                bool tooFewBones = Creature.Constructor.Bones.Count - 1 < Creature.Constructor.MinMaxBones.min;
                if (tooFewBones && CanWarn(bonesWarningAnimator))
                {
                    editorAudioSource.PlayOneShot(errorAudioClip);
                    bonesWarningAnimator.SetTrigger("Warn");
                }
            };
            Creature.Editor.OnTryAddBone += delegate
            {
                bool tooManyBones = Creature.Constructor.Bones.Count + 1 > Creature.Constructor.MinMaxBones.max;
                if (tooManyBones && CanWarn(bonesWarningAnimator))
                {
                    editorAudioSource.PlayOneShot(errorAudioClip);
                    bonesWarningAnimator.SetTrigger("Warn");
                }
            };
        }

        private void OnLocaleChanged(Locale locale)
        {
            UpdateStatistics();
        }
        #endregion

        #region Modes
        public void TryBuild()
        {
            if (!IsBuilding)
            {
                SetMode(EditorMode.Build);
            }
        }
        public void TryPlay()
        {
            if (!IsPlaying)
            {
                SetMode(EditorMode.Play);
            }
        }
        public void TryPaint()
        {
            if (!IsPainting)
            {
                SetMode(EditorMode.Paint);
            }
        }

        public void Build()
        {
            Creature.Spawner.Despawn();
            Creature.Constructor.IsTextured = false;
            Creature.Collider.enabled = false;

            Creature.Animator.enabled = false;

            Creature.Editor.IsDraggable = true;
            Creature.Editor.UseTemporaryOutline = false;
            Creature.Editor.Deselect();
            Creature.Editor.enabled = true;

            Creature.Abilities.enabled = false;
            Creature.Mover.enabled = false;
            Creature.Interactor.enabled = false;
        }
        public void Play()
        {
            Creature.Constructor.Recenter();
            Creature.Constructor.UpdateConfiguration();
            Creature.Constructor.IsTextured = true;

            Creature.Informer.Capture();

            Creature.Collider.UpdateCollider();
            Creature.Collider.enabled = true;

            Creature.Editor.IsDraggable = false;
            Creature.Editor.UseTemporaryOutline = false;
            Creature.Editor.Deselect();
            Creature.Editor.enabled = false;

            Creature.Animator.enabled = true;

            Creature.Abilities.enabled = true;
            Creature.Mover.enabled = true;
            Creature.Interactor.enabled = true;

            Creature.Spawner.Spawn();


#if USE_STATS
            if (Creature.Constructor.Statistics.Weight >= 500f)
            {
                StatsManager.Instance.UnlockAchievement("ACH_HEAVYWEIGHT_CHAMPION");
            }

            if (Creature.Abilities.Abilities.Count >= 15)
            {
                StatsManager.Instance.UnlockAchievement("ACH_OVERPOWERED");
            }

            if (Creature.Mover.MoveSpeed >= 4f)
            {
                StatsManager.Instance.UnlockAchievement("ACH_SPEED_DEMON");
            }
#endif
        }
        public void Paint()
        {
            Creature.Spawner.Despawn();
            Creature.Constructor.IsTextured = true;
            Creature.Collider.enabled = false;

            Creature.Animator.enabled = false;

            Creature.Editor.IsDraggable = false;
            Creature.Editor.UseTemporaryOutline = true;
            Creature.Editor.Deselect();
            Creature.Editor.enabled = true;

            Creature.Abilities.enabled = false;
            Creature.Mover.enabled = false;
            Creature.Interactor.enabled = false;
        }

        public void SetMode(EditorMode mode, bool instant = false)
        {
            switch (mode)
            {
                case EditorMode.Build:
                    buildMenu.Open(instant);
                    playMenu.Close(instant);
                    paintMenu.Close(instant);
                    SetCameraOffset(-1.5f, instant);
                    bodyPartSettingsMenu.Close(true);
                    buildTG.SetIsOnWithoutNotify(true);

                    Build();
                    break;

                case EditorMode.Play:
                    buildMenu.Close(instant);
                    playMenu.Open(instant);
                    paintMenu.Close(instant);
                    SetCameraOffset(0f, instant);
                    playTG.SetIsOnWithoutNotify(true);

                    Play();
                    break;

                case EditorMode.Paint:
                    buildMenu.Close(instant);
                    playMenu.Close(instant);
                    paintMenu.Open(instant);
                    SetCameraOffset(1.5f, instant);
                    patternSettingsMenu.Close(true);
                    colourSettingsMenu.Close(true);
                    paintTG.SetIsOnWithoutNotify(true);

                    Paint();
                    break;
            }

            if (!instant)
            {
                editorAudioSource.PlayOneShot(whooshAudioClip);
            }
        }
        public void SetCameraOffset(float x, bool instant = false)
        {
            Creature.Camera.CameraOrbit.SetOffset(new Vector3(x, 2f, Creature.Camera.CameraOrbit.OffsetPosition.z), instant);
        }
        #endregion

        #region Operations
        public void TrySave()
        {
            UnityAction<string> saveOperation = delegate (string input)
            {
                string savedCreatureName = PreProcessName(input);
                if (IsValidName(savedCreatureName))
                {
                    Creature.Constructor.SetName(savedCreatureName);
                    if (!IsPlaying) // Update only when the configuration could have changed (i.e., when building or painting).
                    {
                        Creature.Constructor.UpdateConfiguration();
                    }

                    bool exists = creaturesUI.Find(x => x.name == savedCreatureName) != null;
                    bool isLoaded = savedCreatureName == Creature.Editor.LoadedCreature;

                    ConfirmOperation(() => Save(Creature.Constructor.Data), exists && !isLoaded, LocalizationUtility.Localize("cc_confirm-overwrite_title"), LocalizationUtility.Localize("cc_confirm-overwrite_message", savedCreatureName));
                }
            };

            CreatureUI selectedCreatureUI = creaturesUI.Find(x => x.SelectToggle.isOn);
            if ((selectedCreatureUI != null) && (selectedCreatureUI.name == Creature.Editor.LoadedCreature))
            {
                saveOperation(selectedCreatureUI.name);
            }
            else
            {
                InputDialog.Input(LocalizationUtility.Localize("cc_creature-name_title"), LocalizationUtility.Localize("cc_creature-name_input"), maxCharacters: 32, submit: LocalizationUtility.Localize("cc_creature-name_submit"), onSubmit: saveOperation);
            }
        }
        public void Save(CreatureData creatureData)
        {
            CreatureUI creatureUI = creaturesUI.Find(x => x.name.Equals(creatureData.Name));
            if (creatureUI == null)
            {
                creatureUI = AddCreatureUI(creatureData.Name);
            }
            creatureUI.SelectToggle.SetIsOnWithoutNotify(true);

            SaveUtility.Save(Path.Combine(creaturesDirectory, $"{creatureData.Name}.dat"), creatureData, creatureEncryptionKey.Value);

            Creature.Editor.LoadedCreature = creatureData.Name;
            Creature.Editor.IsDirty = false;
        }
        public void TryLoad()
        {
            string creatureName = default;

            CreatureUI selectedCreatureUI = creaturesUI.Find(x => x.SelectToggle.isOn);
            if (selectedCreatureUI != null)
            {
                creatureName = selectedCreatureUI.name;
            }
            else return;

            if (!IsValidName(creatureName))
            {
                return;
            }

            CreatureData creatureData = SaveUtility.Load<CreatureData>(Path.Combine(creaturesDirectory, $"{creatureName}.dat"), creatureEncryptionKey.Value);
            ConfirmUnsavedChanges(() => Load(creatureData));
        }
        public void Load(CreatureData creatureData, bool loadFromHistory = false)
        {
            Creature.Mover.Teleport(Creature.Editor.Platform);

            Creature.Animator.enabled = false;
            Creature.Mover.enabled = false;
            Creature.Editor.enabled = true;

            Creature.Editor.Load(creatureData);

            if (IsBuilding)
            {
                Build();
            }
            else
            if (IsPlaying)
            {
                Play();
            }
            else
            if (IsPainting)
            {
                Paint();
            }

            // Colour
            primaryColourPalette.SetColour(Creature.Constructor.Data.PrimaryColour, false);
            secondaryColourPalette.SetColour(Creature.Constructor.Data.SecondaryColour, false);

            // Pattern
            patternsToggleGroup.SetAllTogglesOff(false);
            if (!string.IsNullOrEmpty(Creature.Constructor.Data.PatternID))
            {
                patternsUI.Find(x => x.name.Equals(Creature.Constructor.Data.PatternID)).SelectToggle.SetIsOnWithoutNotify(true);
            }
            patternMaterial.SetColor("_PrimaryCol", primaryColourPalette.Colour);
            patternMaterial.SetColor("_SecondaryCol", secondaryColourPalette.Colour);

            if (!loadFromHistory)
            {
                TakeSnapshot(Change.Load, false);
            }
        }
        public void TryClear()
        {
            ConfirmUnsavedChanges(Clear);
        }
        public void Clear()
        {
            Load(null);

#if USE_STATS
            StatsManager.Instance.UnlockAchievement("ACH_BACK_TO_BASICS");
#endif
        }
        public void TryImport()
        {
            ConfirmUnsavedChanges(delegate
            {
                if (SystemUtility.IsDevice(DeviceType.Desktop))
                {
                    FileBrowser.Instance.OpenSingleFileAsync("dat");
                    FileBrowser.Instance.OnOpenFilesComplete += OnImport;
                }
                else
                if (SystemUtility.IsDevice(DeviceType.Handheld))
                {
                    NativeFilePicker.PickFile(Import, NativeFilePicker.AllFileTypes);
                }
            });
        }
        public void Import(string filePath)
        {
            CreatureData creatureData = SaveUtility.Load<CreatureData>(filePath, creatureEncryptionKey.Value);
            if (creatureData == null)
            {
                creatureData = SaveUtility.Load<CreatureData>(filePath);
            }

            if (CanLoadCreature(creatureData, out string errorTitle, out string errorMessage))
            {
                Save(creatureData);
                Load(creatureData);
            }
            else
            {
                InformationDialog.Inform(errorTitle, errorMessage);
            }
        }
        public void TryExport()
        {
            UnityAction<string, bool> exportDesktopOperation = delegate (string creatureName, bool exportAll)
            {
                string exportedCreatureName = PreProcessName(creatureName);
                if (IsValidName(exportedCreatureName))
                {
                    Creature.Constructor.SetName(exportedCreatureName);
                    if (!IsPlaying)
                    {
                        Creature.Constructor.UpdateConfiguration();
                    }

                    if (exportAll)
                    {
                        FileBrowser.Instance.OnOpenFoldersComplete += OnExportAll;
                    }
                    else
                    {
                        FileBrowser.Instance.OnOpenFoldersComplete += OnExportDat;
                    }
                    FileBrowser.Instance.OpenSingleFolderAsync();
                }
            };

            CreatureUI selectedCreatureUI = creaturesUI.Find(x => x.SelectToggle.isOn);
            if (selectedCreatureUI != null)
            {
                string creatureName = selectedCreatureUI.name;
                if (SystemUtility.IsDevice(DeviceType.Desktop))
                {
                    exportDesktopOperation(creatureName, creatureName == Creature.Editor.LoadedCreature);
                }
                else
                if (SystemUtility.IsDevice(DeviceType.Handheld))
                {
                    NativeFilePicker.ExportFile(Path.Combine(creaturesDirectory, $"{creatureName}.dat"));
                }
            }
            else
            {
                if (SystemUtility.IsDevice(DeviceType.Desktop))
                {
                    InputDialog.Input(LocalizationUtility.Localize("cc_creature-name_title"), LocalizationUtility.Localize("cc_creature-name_input"), maxCharacters: 32, submit: LocalizationUtility.Localize("cc_creature-name_submit"), onSubmit: (string input) => exportDesktopOperation(input, true));
                }
            }
        }
        public void Export(string folderPath, bool exportAll)
        {
            CreatureData data = Creature.Constructor.Data;

            if (exportAll && SettingsManager.Data.PreviewFeatures)
            {
                string creaturePath = Path.Combine(folderPath, data.Name);
                if (!Directory.Exists(creaturePath))
                {
                    Directory.CreateDirectory(creaturePath);
                }

                // Data
                SaveUtility.Save(Path.Combine(creaturePath, $"{data.Name}.dat"), data, creatureEncryptionKey.Value);

                // Screenshot
                Creature.Photographer.TakePhoto(1024, delegate (Texture2D photo)
                {
                    File.WriteAllBytes(Path.Combine(creaturePath, $"{data.Name}.png"), photo.EncodeToPNG());
                });

                // 3D Model
                GameObject export = Creature.Cloner.Clone(data).gameObject;
                export.SetLayerRecursively(LayerMask.NameToLayer("Export"));
                foreach (GameObject tool in export.FindChildrenWithTag("Tool"))
                {
                    tool.SetActive(false);
                }
                FBXExporter.ExportGameObjToFBX(export, Path.Combine(creaturePath, $"{data.Name}.fbx"));
                Destroy(export);
            }
            else
            {
                // Data
                SaveUtility.Save(Path.Combine(folderPath, $"{data.Name}.dat"), data, creatureEncryptionKey.Value);
            }
        }
        public void TryShare(string name)
        {
            string data = Path.Combine(creaturesDirectory, $"{name}.dat");
            CreatureData creatureData = SaveUtility.Load<CreatureData>(data, creatureEncryptionKey.Value);
            if (creatureData != null)
            {
                // Body Parts
                List<BodyPart> bodyParts = new List<BodyPart>();
                foreach (AttachedBodyPart attachedBodyPart in creatureData.AttachedBodyParts)
                {
                    BodyPart bodyPart = DatabaseManager.GetDatabaseEntry<BodyPart>("Body Parts", attachedBodyPart.bodyPartID);
                    if (!bodyParts.Contains(bodyPart))
                    {
                        bodyParts.Add(bodyPart);
                    }
                }
                string bodyPartsStr = bodyParts.Count > 0 ? $"{bodyParts.Count} ({ string.Join(", ", bodyParts)})" : "None";

                // Pattern
                Pattern pattern = DatabaseManager.GetDatabaseEntry<Pattern>("Patterns", creatureData.PatternID);
                string patternStr = pattern?.name ?? "None";

                // Colors
                string colors = $"#{ColorUtility.ToHtmlStringRGB(creatureData.PrimaryColour)} and #{ColorUtility.ToHtmlStringRGB(creatureData.SecondaryColour)}";

                string description =
                    $"Bones: {creatureData.Bones.Count}\n" +
                    $"Body Parts: {bodyPartsStr}\n" +
                    $"Pattern: {patternStr}\n" +
                    $"Tiling: {creatureData.Tiling}\n" +
                    $"Offset: {creatureData.Offset}\n" +
                    $"Colors: {colors}\n" +
                    $"Metallic: {creatureData.Metallic}\n" +
                    $"Shine: {creatureData.Shine}";

                string preview = Path.Combine(creaturesDirectory, $"{name}.png");
                Creature.Photographer.TakePhoto(1024, delegate (Texture2D photo)
                {
                    File.WriteAllBytes(preview, photo.EncodeToPNG());
                    Share(data, preview, name, description);
                },
                creatureData);
            }
        }
        public void Share(string data, string preview, string title, string description)
        {
            if (SystemUtility.IsDevice(DeviceType.Handheld))
            {
                NativeShare share = new NativeShare();
                share.AddFile(data);

                //string link = "";
                //if (Application.platform == RuntimePlatform.Android)
                //{
                //    link = "https://play.google.com/store/apps/details?id=com.daniellochner.creature_creator";
                //}
                //else
                //if (Application.platform == RuntimePlatform.IPhonePlayer)
                //{
                //    link = "https://apps.apple.com/us/app/creature-creator/id1564115819";
                //}
                //else
                //{
                //    link = "https://store.steampowered.com/app/1990050/Creature_Creator/";
                //}
                //share.SetText(LocalizationUtility.Localize("share_subject", link));

                share.SetTitle(title);

                share.Share();
            }
            else
            if (SystemUtility.IsDevice(DeviceType.Desktop))
            {
#if UNITY_STANDALONE
                if (SteamManager.Initialized)
                {
                    string t = LocalizationUtility.Localize("share_title");
                    string m = LocalizationUtility.Localize("share_message", title);
                    ConfirmationDialog.Confirm(t, m, onYes: delegate
                    {
                        currentWorkshopData = new WorkshopData()
                        {
                            dataPath = data,
                            previewPath = preview,
                            title = title,
                            description = description
                        };

                        CallResult<CreateItemResult_t> item = CallResult<CreateItemResult_t>.Create(OnCreateItem);

                        SteamAPICall_t handle = SteamUGC.CreateItem(SteamUtils.GetAppID(), EWorkshopFileType.k_EWorkshopFileTypeCommunity);
                        item.Set(handle);
                    });
                }
#endif
            }
        }

        public bool CanLoadCreature(CreatureData creatureData, out string errorTitle, out string errorMessage)
        {
            #region Invalid
            if (creatureData == null || !IsValidName(creatureData.Name))
            {
                errorTitle = LocalizationUtility.Localize("cc_invalid-creature");
                errorMessage = LocalizationUtility.Localize("cc_cannot-load-creature_reason_invalid");

                return false;
            }
            #endregion

            #region Unavailable
            // Load Conditions
            Pattern pattern = DatabaseManager.GetDatabaseEntry<Pattern>("Patterns", creatureData.PatternID);
            bool patternIsUnlocked = ProgressManager.Data.UnlockedPatterns.Contains(creatureData.PatternID) || CreativeMode || string.IsNullOrEmpty(creatureData.PatternID);
            bool usesPremiumPattern = false;
            if (pattern != null)
            {
                usesPremiumPattern = !PremiumManager.Instance.IsPatternUsable(creatureData.PatternID);
            }

            bool bodyPartsAreUnlocked = true;
            bool usesPremiumBodyPart = false;
            int totalCost = 0, totalComplexity = 0;
            foreach (AttachedBodyPart attachedBodyPart in creatureData.AttachedBodyParts)
            {
                BodyPart bodyPart = DatabaseManager.GetDatabaseEntry<BodyPart>("Body Parts", attachedBodyPart.bodyPartID);
                if (bodyPart != null)
                {
                    totalCost += bodyPart.Price;
                    totalComplexity += bodyPart.Complexity;

                    if (!PremiumManager.Instance.IsBodyPartUsable(attachedBodyPart.bodyPartID))
                    {
                        usesPremiumBodyPart = true;
                    }
                }
                else
                {
                    bodyPartsAreUnlocked = false;
                }

                if (!ProgressManager.Data.UnlockedBodyParts.Contains(attachedBodyPart.bodyPartID) && !CreativeMode)
                {
                    bodyPartsAreUnlocked = false;
                }
            }
            totalComplexity += creatureData.Bones.Count;

            bool creatureIsTooComplicated = totalComplexity > Creature.Constructor.MaxComplexity && !Unlimited;
            bool creatureIsTooExpensive = totalCost > BaseCash && !Unlimited;

            // Error Message
            List<string> errors = new List<string>();
            if (creatureIsTooComplicated)
            {
                errors.Add(LocalizationUtility.Localize("cc_cannot-load-creature_reason_complicated", totalComplexity, Creature.Constructor.MaxComplexity));
            }
            if (creatureIsTooExpensive)
            {
                errors.Add(LocalizationUtility.Localize("cc_cannot-load-creature_reason_expensive", totalCost, BaseCash));
            }
            if (usesPremiumPattern)
            {
                errors.Add(LocalizationUtility.Localize("cc_cannot-load-creature_reason_premium-pattern"));
            }
            else if (!patternIsUnlocked)
            {
                errors.Add(LocalizationUtility.Localize("cc_cannot-load-creature_reason_patterns"));
            }
            if (usesPremiumBodyPart)
            {
                errors.Add(LocalizationUtility.Localize("cc_cannot-load-creature_reason_premium-body-parts"));
            }
            else if (!bodyPartsAreUnlocked)
            {
                errors.Add(LocalizationUtility.Localize("cc_cannot-load-creature_reason_body-parts"));
            }

            errorTitle = LocalizationUtility.Localize("cc_creature-unavailable");
            errorMessage = LocalizationUtility.Localize("cc_cannot-load-creature_reason", creatureData.Name) + "\n";
            if (errors.Count > 0)
            {
                for (int i = 0; i < errors.Count; i++)
                {
                    errorMessage += $"{i + 1}. {errors[i]}\n";
                }
                return false;
            }
            else
            {
                return true;
            }
            #endregion
        }
        public bool CanAddBodyPart(string bodyPartID)
        {
            BodyPart bodyPart = DatabaseManager.GetDatabaseEntry<BodyPart>("Body Parts", bodyPartID);

            bool tooComplicated = Creature.Constructor.Statistics.Complexity + bodyPart.Complexity > Creature.Constructor.MaxComplexity && !Unlimited;
            bool notEnoughCash = Creature.Editor.Cash < bodyPart.Price && !Unlimited;
            bool isPremium = !PremiumManager.Instance.IsBodyPartUsable(bodyPartID);

            if (notEnoughCash || tooComplicated || isPremium)
            {
                editorAudioSource.PlayOneShot(errorAudioClip);

                if (isPremium)
                {
                    PremiumMenu.Instance.RequestBodyPart(bodyPartID);
                }
                else
                {
                    if (notEnoughCash && CanWarn(cashWarningAnimator))
                    {
                        cashWarningAnimator.SetTrigger("Warn");
                    }
                    if (tooComplicated && CanWarn(complexityWarningAnimator))
                    {
                        complexityWarningAnimator.SetTrigger("Warn");
                    }
                }
                return false;
            }
            else
            {
                return true;
            }
        }
        public bool CanAddPattern(string patternID)
        {
            bool isPremium = !PremiumManager.Instance.IsPatternUsable(patternID);

            if (isPremium)
            {
                editorAudioSource.PlayOneShot(errorAudioClip);

                PremiumMenu.Instance.RequestPattern(patternID);
                return false;
            }
            else
            {
                return true;
            }
        }

        private void OnExportDat(bool selected, string singleFolder, string[] folders)
        {
            if (selected)
            {
                Export(singleFolder, false);
            }
            FileBrowser.Instance.OnOpenFoldersComplete -= OnExportDat;
        }
        private void OnExportAll(bool selected, string singleFolder, string[] folders)
        {
            if (selected)
            {
                Export(singleFolder, true);
            }
            FileBrowser.Instance.OnOpenFoldersComplete -= OnExportAll;
        }
        private void OnImport(bool selected, string singleFile, string[] files)
        {
            if (selected)
            {
                Import(singleFile);
            }
            FileBrowser.Instance.OnOpenFilesComplete -= OnImport;
        }

#if UNITY_STANDALONE
        private void OnCreateItem(CreateItemResult_t item, bool hasFailed)
        {
            if (hasFailed)
            {
                return;
            }
            if (item.m_bUserNeedsToAcceptWorkshopLegalAgreement)
            {
                SteamFriends.ActivateGameOverlayToWebPage("https://steamcommunity.com/workshop/workshoplegalagreement/");
                return;
            }

            handle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), item.m_nPublishedFileId);
            SteamUGC.SetItemTitle(handle, currentWorkshopData.title);
            SteamUGC.SetItemDescription(handle, currentWorkshopData.description);
            SteamUGC.SetItemContent(handle, currentWorkshopData.dataPath);
            SteamUGC.SetItemPreview(handle, currentWorkshopData.previewPath);
            SteamUGC.SetItemVisibility(handle, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic);
            SteamUGC.SubmitItemUpdate(handle, null);

            StartCoroutine(UploadItemRoutine(item.m_nPublishedFileId));
        }
        private IEnumerator UploadItemRoutine(PublishedFileId_t pf)
        {
            currentCreatureUI.IsSharing = true;

            while (true)
            {
                EItemUpdateStatus status = SteamUGC.GetItemUpdateProgress(handle, out ulong p, out ulong t);
                if (status == EItemUpdateStatus.k_EItemUpdateStatusInvalid)
                {
                    break;
                }
                yield return null;
            }
            SteamFriends.ActivateGameOverlayToWebPage($"steam://url/CommunityFilePage/{pf}");

            currentCreatureUI.IsSharing = false;
        }
#endif
        #endregion

        #region Unlocks
        public void UnlockPattern(string patternID, bool notify = true)
        {
            if (ProgressManager.Data.UnlockedPatterns.Contains(patternID) || CreativeMode) return;

            ProgressManager.Data.UnlockedPatterns.Add(patternID);

            if (notify)
            {
                Pattern pattern = DatabaseManager.GetDatabaseEntry<Pattern>("Patterns", patternID);
                Sprite icon = Sprite.Create(pattern.Texture, new Rect(0, 0, pattern.Texture.width, pattern.Texture.height), new Vector2(0.5f, 0.5f));
                string title = pattern.name;
                string description = $"{LocalizationUtility.Localize("cc_unlocked_pattern")} ({ProgressManager.Data.UnlockedPatterns.Count}/{DatabaseManager.GetDatabase("Patterns").Objects.Count})";
                UnityAction onClose = () => UnlockablePatternsMenu.Instance.Open();
                float iconScale = 0.8f;
                NotificationsManager.Notify(icon, title, description, onClose, iconScale);
            }

            AddPatternUI(patternID, false, true);
        }
        public void UnlockBodyPart(string bodyPartID, bool notify = true)
        {
            if (ProgressManager.Data.UnlockedBodyParts.Contains(bodyPartID) || CreativeMode) return;

            ProgressManager.Data.UnlockedBodyParts.Add(bodyPartID);

            if (notify)
            {
                BodyPart bodyPart = DatabaseManager.GetDatabaseEntry<BodyPart>("Body Parts", bodyPartID);
                Sprite icon = bodyPart.Icon;
                string title = bodyPart.name;
                string description = $"{LocalizationUtility.Localize("cc_unlocked_body-part")} ({ProgressManager.Data.UnlockedBodyParts.Count}/{DatabaseManager.GetDatabase("Body Parts").Objects.Count})";
                UnityAction onClose = () => UnlockableBodyPartsMenu.Instance.Open();
                NotificationsManager.Notify(icon, title, description, onClose);
            }

            AddBodyPartUI(bodyPartID, true, true);
        }
        public void UnlockRandomBodyPart()
        {
            Database bodyParts = DatabaseManager.GetDatabase("Body Parts");
            UnlockBodyPart((bodyParts.Objects.ToArray()[UnityEngine.Random.Range(0, bodyParts.Objects.Count)]).Key);
        }
        public void UnlockRandomPattern()
        {
            Database patterns = DatabaseManager.GetDatabase("Patterns");
            UnlockPattern((patterns.Objects.ToArray()[UnityEngine.Random.Range(0, patterns.Objects.Count)]).Key);
        }
        #endregion

        #region UI
        public CreatureUI AddCreatureUI(string creatureName)
        {
            CreatureUI creatureUI = Instantiate(creatureUIPrefab, creaturesRT);
            creaturesUI.Add(creatureUI);
            creatureUI.Setup(creatureName);

            creatureUI.SelectToggle.group = creaturesToggleGroup;

            creatureUI.RemoveButton.onClick.AddListener(delegate
            {
                ConfirmationDialog.Confirm(LocalizationUtility.Localize("cc_remove-creature_title"), LocalizationUtility.Localize("cc_remove-creature_message", creatureName), onYes: delegate
                {
                    creaturesUI.Remove(creatureUI);
                    Destroy(creatureUI.gameObject);
                    File.Delete(Path.Combine(creaturesDirectory, $"{creatureName}.dat"));

                    if (creatureName.Equals(Creature.Editor.LoadedCreature))
                    {
                        Creature.Editor.LoadedCreature = "";
                        Creature.Editor.IsDirty = false;
                    }

                    UpdateNoCreatures();
                });
            });
            UpdateNoCreatures();
            FilterCreatures(creatureNameText.text);

            creatureUI.ShareButton.onClick.AddListener(delegate
            {
                currentCreatureUI = creatureUI;
                TryShare(creatureName);
            });

            return creatureUI;
        }
        public BodyPartUI AddBodyPartUI(string bodyPartID, bool update = false, bool isNew = false)
        {
            BodyPart bodyPart = DatabaseManager.GetDatabaseEntry<BodyPart>("Body Parts", bodyPartID);

            GridLayoutGroup grid = bodyPartGrids[bodyPart.PluralForm].grid;
            if (!grid.gameObject.activeSelf)
            {
                int index = grid.transform.GetSiblingIndex();
                grid.transform.parent.GetChild(index).gameObject.SetActive(true);
                grid.transform.parent.GetChild(index - 1).gameObject.SetActive(true);
            }

            BodyPartUI bodyPartUI = Instantiate(bodyPartUIPrefab, grid.transform as RectTransform);
            bodyPartUI.Setup(bodyPart, isNew);
            bodyPartUI.name = bodyPartID;
            bodyPartUI.SetUsable(PremiumManager.Instance.IsBodyPartUsable(bodyPartID));
            noPartsText.SetActive(false);
            grid.CalculateLayoutInputVertical();

            bodyPartUI.DragUI.OnRelease.AddListener(delegate
            {
                grid.CalculateLayoutInputVertical();
            });
            bodyPartUI.DragUI.OnDrag.AddListener(delegate
            {
                if (!CanAddBodyPart(bodyPartID))
                {
                    bodyPartUI.DragUI.OnPointerUp(null);
                }

                if (!RectTransformUtility.RectangleContainsScreenPoint(bodyPartsRT, Input.mousePosition) && !CanvasUtility.IsPointerOverUI)
                {
                    bodyPartUI.DragUI.OnPointerUp(null);
                    editorAudioSource.PlayOneShot(createAudioClip);

                    Ray ray = Creature.Camera.MainCamera.ScreenPointToRay(Input.mousePosition);
                    Plane plane = new Plane(Creature.Camera.MainCamera.transform.forward, Creature.Editor.Platform.transform.position);

                    if (plane.Raycast(ray, out float distance))
                    {
                        BodyPartConstructor main = Creature.Constructor.AddBodyPart(bodyPartID);
                        main.transform.position = ray.GetPoint(distance);
                        main.transform.rotation = Quaternion.LookRotation(-plane.normal, Creature.Constructor.transform.up);
                        main.transform.parent = Dynamic.Transform;

                        main.SetupAttachment(new AttachedBodyPart(bodyPartID));

                        main.Flipped.gameObject.SetActive(false);
                        BodyPartEditor bpe = main.GetComponent<BodyPartEditor>();
                        if (bpe != null)
                        {
                            bpe.LDrag.OnMouseButtonDown();
                            bpe.LDrag.Plane = plane;
                        }
                    }

#if USE_STATS
                    StatsManager.Instance.CashSpent += bodyPart.Price;

                    if (bodyPart is Eye)
                    {
                        StatsManager.Instance.UnlockAchievement("ACH_I_CAN_SEE_CLEARLY_NOW");
                    }
#endif
                }
            });

            bodyPartUI.ClickUI.OnRightClick.AddListener(delegate
            {
                ConfirmationDialog.Confirm(LocalizationUtility.Localize("cc_hide-body-part_title"), LocalizationUtility.Localize("cc_hide-body-part_message", bodyPart.name), onYes: delegate
                {
                    RemoveBodyPartUI(bodyPartUI);
                    SettingsManager.Data.HiddenBodyParts.Add(bodyPartID);
                });
            });

            if (update)
            {
                UpdateBodyPartTotals();
            }

            return bodyPartUI;
        }
        public PatternUI AddPatternUI(string patternID, bool update = false, bool isNew = false)
        {
            Pattern pattern = DatabaseManager.GetDatabaseEntry<Pattern>("Patterns", patternID);

            PatternUI patternUI = Instantiate(patternUIPrefab, patternsRT.transform);
            patternsUI.Add(patternUI);
            patternUI.Setup(pattern, patternMaterial, isNew);
            patternUI.name = patternID;
            patternUI.SetUsable(PremiumManager.Instance.IsPatternUsable(patternID));
            noPatternsText.SetActive(false);

            patternUI.SelectToggle.group = patternsToggleGroup;
            patternUI.SelectToggle.onValueChanged.AddListener(delegate (bool isSelected)
            {
                if (!isSelected || CanAddPattern(patternID))
                {
                    string nextPatternID = (isSelected ? patternID : "");
                    Creature.Constructor.SetPattern(nextPatternID);

                    TakeSnapshot(Change.SetPattern);
                }
                else
                {
                    patternUI.SelectToggle.SetIsOnWithoutNotify(false);
                }
            });

            patternUI.ClickUI.OnRightClick.AddListener(delegate
            {
                ConfirmationDialog.Confirm(LocalizationUtility.Localize("cc_hide-pattern_title"), LocalizationUtility.Localize("cc_hide-pattern_message", pattern.name), onYes: delegate
                {
                    RemovePatternUI(patternUI);
                    SettingsManager.Data.HiddenPatterns.Add(patternID);
                });
            });

            return patternUI;
        }

        public void RemoveBodyPartUI(BodyPartUI bodyPartUI)
        {
            Transform grid = bodyPartUI.transform.parent;
            if (grid.childCount == 1)
            {
                int index = grid.GetSiblingIndex();
                grid.parent.GetChild(index).gameObject.SetActive(false);
                grid.parent.GetChild(index - 1).gameObject.SetActive(false);
            }

            Destroy(bodyPartUI.gameObject);
            this.InvokeAtEndOfFrame(UpdateBodyPartTotals);
        }
        public void RemovePatternUI(PatternUI patternUI)
        {
            Destroy(patternUI.gameObject);
        }

        public void SetPatternUI(string patternID)
        {
            if (patternID != "")
            {
                patternsRT.Find(patternID).GetComponent<Toggle>().SetIsOnWithoutNotify(true);
            }
            else
            {
                patternsToggleGroup.GetFirstActiveToggle().SetIsOnWithoutNotify(false);
            }
        }
        public void SetTilingUI(Vector2 tiling)
        {
            patternMaterial.SetTextureScale("_MainTex", tiling);
            patternSettingsMenu.TilingX.SetTextWithoutNotify(tiling.x.ToString());
            patternSettingsMenu.TilingY.SetTextWithoutNotify(tiling.y.ToString());
        }
        public void SetOffsetUI(Vector2 offset)
        {
            patternMaterial.SetTextureOffset("_MainTex", offset);
            patternSettingsMenu.OffsetX.SetTextWithoutNotify(offset.x.ToString());
            patternSettingsMenu.OffsetY.SetTextWithoutNotify(offset.y.ToString());
        }
        public void SetShineUI(float shine)
        {
            colourSettingsMenu.ShineSlider.SetValueWithoutNotify(shine);
        }
        public void SetMetallicUI(float metallic)
        {
            colourSettingsMenu.MetallicSlider.SetValueWithoutNotify(metallic);
        }

        public void SetPrimaryColourUI(Color colour, bool isOverride)
        {
            primaryColourPalette.SetColour(colour);
            primaryColourPalette.gameObject.SetActive(colour.a != 0);
            SetPrimaryColourOverrideUI(isOverride);

            SetColourNoneUI();
        }
        public void SetPrimaryColourOverrideUI(bool isOverride)
        {
            primaryColourOverride.SetActive(isOverride);
        }
        public void SetSecondaryColourUI(Color colour, bool isOverride)
        {
            secondaryColourPalette.SetColour(colour);
            secondaryColourPalette.gameObject.SetActive(colour.a != 0);
            SetSecondaryColourOverrideUI(isOverride);

            SetColourNoneUI();
        }
        public void SetSecondaryColourOverrideUI(bool isOverride)
        {
            secondaryColourOverride.SetActive(isOverride);
        }
        private void SetColourNoneUI()
        {
            noColoursText.gameObject.SetActive(!primaryColourPalette.gameObject.activeSelf && !secondaryColourPalette.gameObject.activeSelf);
        }

        public void FilterCreatures(string filterText)
        {
            filterText = filterText.ToLower();
            foreach (CreatureUI creatureUI in creaturesUI)
            {
                bool filtered = false;
                if (!string.IsNullOrEmpty(filterText))
                {
                    filtered = true;
                    foreach (TextMeshProUGUI tmp in creatureUI.GetComponentsInChildren<TextMeshProUGUI>())
                    {
                        string text = tmp.text.ToLower();
                        if (text.Contains(filterText.ToLower()))
                        {
                            filtered = false;
                            break;
                        }
                    }
                }
                creatureUI.gameObject.SetActive(!filtered);

                if (filtered)
                {
                    creatureUI.SelectToggle.SetIsOnWithoutNotify(false);
                }
            }
        }

        public void UpdateUsability()
        {
            foreach (BodyPartUI bodyPartUI in bodyPartsRT.GetComponentsInChildren<BodyPartUI>())
            {
                bodyPartUI.SetUsable(PremiumManager.Instance.IsBodyPartUsable(bodyPartUI.name));
            }
            foreach (PatternUI patternUI in patternsRT.GetComponentsInChildren<PatternUI>())
            {
                patternUI.SetUsable(PremiumManager.Instance.IsPatternUsable(patternUI.name));
            }
        }
        public void UpdateBodyPartTotals()
        {
            foreach (string type in bodyPartGrids.Keys)
            {
                int count = bodyPartGrids[type].grid.transform.childCount;
                TextMeshProUGUI title = bodyPartGrids[type].title;
                title.SetArguments(count);
                title.gameObject.SetActive(count > 0);
            }
        }
        public void UpdatePrimaryColour()
        {
            Color colour = primaryColourPalette.Colour;
            BodyPartEditor bodyPart = Creature.Editor.PaintedBodyPart;
            if (bodyPart)
            {
                bodyPart.BodyPartConstructor.SetPrimaryColour(colour);
                SetPrimaryColourOverrideUI(bodyPart.BodyPartConstructor.IsPrimaryOverridden);
            }
            else
            {
                Creature.Constructor.SetPrimaryColour(colour);
                patternMaterial.SetColor("_PrimaryCol", colour);
                SetPrimaryColourOverrideUI(false);

                TakeSnapshot(Change.SetBodyPrimaryColor);
            }
        }
        public void UpdateSecondaryColour()
        {
            Color colour = secondaryColourPalette.Colour;
            BodyPartEditor bodyPart = Creature.Editor.PaintedBodyPart;
            if (bodyPart)
            {
                bodyPart.BodyPartConstructor.SetSecondaryColour(colour);
                SetSecondaryColourOverrideUI(bodyPart.BodyPartConstructor.IsSecondaryOverridden);
            }
            else
            {
                Creature.Constructor.SetSecondaryColour(secondaryColourPalette.Colour);
                patternMaterial.SetColor("_SecondaryCol", colour);
                SetSecondaryColourOverrideUI(false);

                TakeSnapshot(Change.SetBodySecondaryColor);
            }
        }
        public void UpdateStatistics()
        {
            CreatureStatistics statistics = Creature.Constructor.Statistics;
            CreatureDimensions dimensions = Creature.Constructor.Dimensions;

            complexityText.SetArguments(statistics.Complexity, Unlimited ? "∞" : Creature.Constructor.MaxComplexity.ToString());
            heightText.SetArguments(Math.Round(dimensions.height, 2));
            weightText.SetArguments(Math.Round(statistics.Weight, 2));
            dietText.SetArguments(LocalizationUtility.Localize($"diet_{statistics.Diet}".ToLower()));
            healthText.SetArguments(statistics.Health);
            speedText.SetArguments(Math.Round(Creature.Mover.MoveSpeed, 2));
            bonesText.SetArguments(Creature.Constructor.Bones.Count, Creature.Constructor.MinMaxBones.max);

            bodyPartsToggle.onValueChanged.Invoke(bodyPartsToggle.isOn);
            abilitiesToggle.onValueChanged.Invoke(abilitiesToggle.isOn);

            cashText.text = $"${(Unlimited ? "∞" : Creature.Editor.Cash.ToString())}";
        }
        public void UpdateCreaturesFormatting()
        {
            foreach (CreatureUI creatureUI in creaturesUI)
            {
                creatureUI.NameText.text = creatureUI.NameText.text.Replace("<u>", "").Replace("</u>", "").Replace("*", ""); // Clear all formatting.

                if (!string.IsNullOrEmpty(Creature.Editor.LoadedCreature) && creatureUI.NameText.text.Equals(Creature.Editor.LoadedCreature))
                {
                    creatureUI.NameText.text = $"<u>{creatureUI.NameText.text}</u>{(Creature.Editor.IsDirty ? "*" : "")}";
                }
            }
        }
        public void UpdateLoadableCreatures()
        {
            StartCoroutine(UpdateLoadableCreaturesRoutine());
        }
        public void UpdateNoCreatures()
        {
            noCreaturesText.SetActive(creaturesUI.Count == 0);
        }

        /// <summary>
        /// Use a coroutine to prevent lag spikes when entering a platform!
        /// </summary>
        private IEnumerator UpdateLoadableCreaturesRoutine()
        {
            foreach (CreatureUI creatureUI in creaturesUI)
            {
                UpdateLoadableCreatureUI(creatureUI);
                yield return null;
            }
        }
        private void UpdateLoadableCreatureUI(CreatureUI creatureUI)
        {
            CreatureData creatureData = SaveUtility.Load<CreatureData>(Path.Combine(creaturesDirectory, $"{creatureUI.name}.dat"), creatureEncryptionKey.Value);

            bool canLoadCreature = CanLoadCreature(creatureData, out string errorTitle, out string errorMessage);

            // Button
            creatureUI.ErrorButton.gameObject.SetActive(!canLoadCreature);
            if (!canLoadCreature)
            {
                creatureUI.ErrorButton.onClick.RemoveAllListeners();
                creatureUI.ErrorButton.onClick.AddListener(delegate
                {
                    InformationDialog.Inform(errorTitle, errorMessage);
                });
            }

            // Background
            Color colour = canLoadCreature ? Color.white : Color.black;
            colour.a = 0.25f;
            creatureUI.SelectToggle.targetGraphic.color = colour;
            creatureUI.SelectToggle.interactable = canLoadCreature;
        }

        public void SetEditing(bool e)
        {
            isEditing = e;

            optionsSideMenu.Close();

            StartCoroutine(paginationCanvasGroup.Fade(isEditing, 0.25f));
            StartCoroutine(optionsCanvasGroup.Fade(isEditing, 0.25f));
        }
        public void SetVisibility(bool v, float t = 0.25f)
        {
            isVisible = v;
            if (visibleCoroutine != null)
            {
                StopCoroutine(visibleCoroutine);
            }
            visibleCoroutine = StartCoroutine(editorCanvasGroup.Fade(v, t));
        }
        #endregion

        #region History
        public void TakeSnapshot(Change change, float delay)
        {
            if (delayedRecordCoroutine != null && change == prevDelayedChangeType)
            {
                StopCoroutine(delayedRecordCoroutine);
            }
            delayedRecordCoroutine = this.Invoke(delegate
            {
                TakeSnapshot(change);
            },
            delay);

            prevDelayedChangeType = change;
        }
        public void TakeSnapshot(Change change, bool setDirty = true)
        {
            string data = JsonUtility.ToJson(Creature.Constructor.Data);
            if (Counter == -1 || !data.Equals(History[Counter]))
            {
                if (History.Count > Counter + 1)
                {
                    while (History.Count > Counter + 1)
                    {
                        History.RemoveAt(Counter + 1);
                    }
                    SetRedoable(false);
                }

                History.Add(new ChangeData(change, data));
                Counter++;

                if (History.Count > historyLimit)
                {
                    History.RemoveAt(0);
                    Counter--;
                }

                if (setDirty)
                {
                    Creature.Editor.IsDirty = true;
                }

                SetUndoable(Counter > 0);
            }
        }
        public void Undo()
        {
            if (Counter > 0)
            {
                LoadFromHistory(--Counter);

                SetUndoable(Counter > 0);
                SetRedoable(true);
            }
        }
        public void Redo()
        {
            if (Counter < History.Count - 1)
            {
                LoadFromHistory(++Counter);

                SetRedoable(Counter < History.Count - 1);
                SetUndoable(true);
            }
        }

        public void LoadFromHistory(int index)
        {
            CreatureData data = JsonUtility.FromJson<CreatureData>(History[index].data);
            Load(data, true);
        }
        public void ClearHistory()
        {
            History.Clear();
            Counter = -1;
        }

        private void SetUndoable(bool canUndo)
        {
            undoBuildCG.interactable = undoPaintCG.interactable = canUndo;
            undoBuildCG.alpha = undoPaintCG.alpha = canUndo ? 1f : 0.25f;
        }
        private void SetRedoable(bool canRedo)
        {
            redoBuildCG.interactable = redoPaintCG.interactable = canRedo;
            redoBuildCG.alpha = redoPaintCG.alpha = canRedo ? 1f : 0.25f;
        }
        #endregion

        #region Keyboard Shortcuts
        private void HandleKeyboardShortcuts()
        {
            if (!Creature || Creature.Health.IsDead) return;

            if (IsBuilding)
            {
                HandleBuildShortcuts();
            }
            else
            if (IsPlaying)
            {
                HandlePlayShortcuts();
            }
            else
            if (IsPainting)
            {
                HandlePaintShortcuts();
            }

            if (IsEditing)
            {
                HandleEditorShortcuts();
            }
        }

        private void HandleBuildShortcuts()
        {
            if (InputUtility.GetKeyDown(KeybindingsManager.Data.Undo))
            {
                Undo();
            }
            else
            if (InputUtility.GetKeyDown(KeybindingsManager.Data.Redo))
            {
                Redo();
            }
            else
            if (InputUtility.GetKeyDown(KeybindingsManager.Data.Flip))
            {
                Player.Instance.Constructor.Flip();
            }
        }
        private void HandlePlayShortcuts()
        {
            if (InputUtility.GetKeyDown(KeybindingsManager.Data.ToggleUI))
            {
                SetVisibility(!isVisible);
                Creature.Camera.ToolCamera.cullingMask = isVisible ? LayerMask.GetMask("UI", "Tools") : 0;
            }
        }
        private void HandlePaintShortcuts()
        {
        }

        private void HandleEditorShortcuts()
        {
            if (InputUtility.GetKeyDown(KeybindingsManager.Data.Save))
            {
                TrySave();
            }
            else
            if (InputUtility.GetKeyDown(KeybindingsManager.Data.Export))
            {
                TryExport();
            }
            else
            if (InputUtility.GetKeyDown(KeybindingsManager.Data.Import))
            {
                TryImport();
            }
            else
            if (InputUtility.GetKeyDown(KeybindingsManager.Data.Clear) && EventSystem.current.currentSelectedGameObject == null) // Must still allow for copying of UI text (e.g., World ID)
            {
                TryClear();
            }
        }
        #endregion

        #region Helper
        private bool CanWarn(Animator warnAnimator)
        {
            return warnAnimator.CanTransitionTo("Warn") && !warnAnimator.IsInTransition(0) && !warnAnimator.GetCurrentAnimatorStateInfo(0).IsName("Warn");
        }

        /// <summary>
        /// Is this a valid name for a creature? Checks for invalid file name characters and profanity, and corrects when possible.
        /// </summary>
        /// <param name="creatureName">The name to be validated.</param>
        /// <returns>The validity of the name.</returns>
        private bool IsValidName(string creatureName)
        {
            if (string.IsNullOrEmpty(creatureName))
            {
                InformationDialog.Inform(LocalizationUtility.Localize("cc_creature-unnamed_title"), LocalizationUtility.Localize("cc_creature-unnamed_message"));
                return false;
            }

            if (checkForProfanity)
            {
                ProfanityFilter filter = new ProfanityFilter();
                if (filter.ContainsProfanity(creatureName))
                {
                    IReadOnlyCollection<string> profanities = filter.DetectAllProfanities(creatureName);
                    if (profanities.Count > 0)
                    {
                        InformationDialog.Inform(LocalizationUtility.Localize("cc_profanity-detected"), LocalizationUtility.Localize("cc_remove-profanity", string.Join(", ", profanities)));
                    }
                    else
                    {
                        InformationDialog.Inform(LocalizationUtility.Localize("cc_profanity-detected"), LocalizationUtility.Localize("cc_profane-terms"));
                    }
                    return false;
                }
            }

            return true;
        }

        private string PreProcessName(string creatureName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                creatureName = creatureName.Replace(c.ToString(), "");
            }
            creatureName = creatureName.Trim();

            return creatureName;
        }

        /// <summary>
        /// Prompts the user to confirm before performing an operation.
        /// </summary>
        /// <param name="operation">The operation to be performed.</param>
        /// <param name="confirm">Whether or not confirmation is necessary.</param>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="message">The message of the dialog.</param>
        private void ConfirmOperation(UnityAction operation, bool confirm, string title, string message)
        {
            if (confirm)
            {
                ConfirmationDialog.Confirm(title, message, onYes: operation);
            }
            else
            {
                operation();
            }
        }
        private void ConfirmUnsavedChanges(UnityAction operation)
        {
            ConfirmOperation(operation, Creature.Editor.IsDirty && !string.IsNullOrEmpty(Creature.Editor.LoadedCreature), LocalizationUtility.Localize("cc_confirm-unsaved"), LocalizationUtility.Localize("cc_confirm-unsaved_message", Creature.Editor.LoadedCreature));
        }
        #endregion
        #endregion

        #region Enums
        public enum EditorMode
        {
            Build,
            Play,
            Paint
        }
        #endregion

        #region Nested
        [Serializable]
        public struct ChangeData
        {
            public Change change;
            public string data;

            public ChangeData(Change change, string data)
            {
                this.change = change;
                this.data = data;
            }
        }

        [Serializable]
        public struct GridInfo
        {
            public TextMeshProUGUI title;
            public GridLayoutGroup grid;
        }

        [Serializable]
        public struct WorkshopData
        {
            public string dataPath;
            public string previewPath;
            public string title;
            public string description;
        }

        [Serializable]
        public class BodyPartGrids : SerializableDictionaryBase<string, GridInfo> { }
        #endregion
    }
}