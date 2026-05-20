using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nocturne;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class NocturneVillageSceneBuilder
{
    private const string BuildVersion = "2.3.0";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string FlagPath = "Assets/NocturneVillageBuilt.flag";
    private const string MaterialsFolder = "Assets/Materials";
    private const string ClipsFolder = "Assets/Animations/Clips";
    private const string ControllersFolder = "Assets/Animations/Controllers";
    private const string CharacterPrefabFolder = "Assets/Prefabs/Characters";
    private const string EnvironmentPrefabFolder = "Assets/Prefabs/Environment";
    private const string EffectsPrefabFolder = "Assets/Prefabs/Effects";
    private const string GameplayPrefabFolder = "Assets/Prefabs/Gameplay";
    private const string CharacterModelsFolder = "Assets/Models/Characters";

    [InitializeOnLoadMethod]
    private static void AutoBuildIfNeeded()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        EditorApplication.delayCall += PrepareBuildAttempt;
    }

    private static void PrepareBuildAttempt()
    {
        if (!NeedsBuild())
        {
            return;
        }

        if (Application.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        EditorApplication.delayCall += TryAutoBuild;
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += TryAutoBuild;
        }
    }

    [MenuItem("Tools/Nocturne Village/Rebuild Scene")]
    private static void RebuildScene()
    {
        DeleteAssetIfPresent(FlagPath);
        BuildScene();
    }

    private static void TryAutoBuild()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryAutoBuild;
            return;
        }

        if (Application.isPlaying)
        {
            return;
        }

        if (NeedsBuild())
        {
            BuildScene();
        }
    }

    private static bool NeedsBuild()
    {
        string fullFlagPath = ToAbsoluteProjectPath(FlagPath);
        return !File.Exists(fullFlagPath) || File.ReadAllText(fullFlagPath).Trim() != BuildVersion;
    }

    private static void BuildScene()
    {
        try
        {
            EnsureFolders();
            EnsureHumanoidImports();
            CreateAnimationAssets();
            CreateEnvironmentPrefabs();
            CreateEffectPrefabs();
            CreateGameplayPrefabs();
            CreateCharacterPrefabs();
            BuildSceneContent();
            File.WriteAllText(ToAbsoluteProjectPath(FlagPath), BuildVersion);
            AssetDatabase.ImportAsset(FlagPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[NocturneBuilder] Scene hazir.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[NocturneBuilder] Build failed: {exception}");
        }
    }

    private static void EnsureFolders()
    {
        string[] folders =
        {
            MaterialsFolder,
            ClipsFolder,
            ControllersFolder,
            CharacterPrefabFolder,
            EnvironmentPrefabFolder,
            EffectsPrefabFolder,
            GameplayPrefabFolder
        };

        foreach (string folder in folders)
        {
            string absolutePath = ToAbsoluteProjectPath(folder);
            if (!Directory.Exists(absolutePath))
            {
                Directory.CreateDirectory(absolutePath);
                AssetDatabase.ImportAsset(folder);
            }
        }
    }

    private static void EnsureHumanoidImports()
    {
        string remyBase = $"{CharacterModelsFolder}/Remy@Idle.fbx";
        string girlBase = $"{CharacterModelsFolder}/Peasant Girl.fbx";
        string vampireBase = $"{CharacterModelsFolder}/Vampire A Lusth@Idle.fbx";

        Avatar remyAvatar = ConfigureHumanoidModel(remyBase, null);
        Avatar girlAvatar = ConfigureHumanoidModel(girlBase, null);
        Avatar vampireAvatar = ConfigureHumanoidModel(vampireBase, null);

        ConfigureHumanoidModel($"{CharacterModelsFolder}/Remy@Running.fbx", remyAvatar);
        ConfigureHumanoidModel($"{CharacterModelsFolder}/Remy@Double Dagger Stab.fbx", remyAvatar);
        ConfigureHumanoidModel($"{CharacterModelsFolder}/Peasant Girl@Walking.fbx", girlAvatar);
        ConfigureHumanoidModel($"{CharacterModelsFolder}/Peasant Girl@Running.fbx", girlAvatar);
        ConfigureHumanoidModel($"{CharacterModelsFolder}/Peasant Girl@Praying.fbx", girlAvatar);
        ConfigureHumanoidModel($"{CharacterModelsFolder}/Vampire A Lusth@Walking.fbx", vampireAvatar);
        ConfigureHumanoidModel($"{CharacterModelsFolder}/Vampire A Lusth@Vampiric Bite.fbx", vampireAvatar);
        ConfigureHumanoidModel($"{CharacterModelsFolder}/Vampire A Lusth@Drop Kick.fbx", vampireAvatar);
    }

    private static Avatar ConfigureHumanoidModel(string assetPath, Avatar sourceAvatar)
    {
        ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
        {
            throw new FileNotFoundException($"Model not found: {assetPath}");
        }

        importer.globalScale = 1f;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.importCameras = false;
        importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.optimizeGameObjects = false;
        importer.avatarSetup = sourceAvatar == null
            ? ModelImporterAvatarSetup.CreateFromThisModel
            : ModelImporterAvatarSetup.CopyFromOther;

        if (sourceAvatar != null)
        {
            importer.sourceAvatar = sourceAvatar;
        }

        importer.SaveAndReimport();
        return LoadAvatar(assetPath);
    }

    private static Avatar LoadAvatar(string assetPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<Avatar>()
            .FirstOrDefault(avatar => avatar.isHuman);
    }

    private static void CreateAnimationAssets()
    {
        ConfigureClipLoop($"{CharacterModelsFolder}/Remy@Idle.fbx", true);
        ConfigureClipLoop($"{CharacterModelsFolder}/Remy@Running.fbx", true);
        ConfigureClipLoop($"{CharacterModelsFolder}/Peasant Girl@Praying.fbx", true);
        ConfigureClipLoop($"{CharacterModelsFolder}/Peasant Girl@Running.fbx", true);
        ConfigureClipLoop($"{CharacterModelsFolder}/Vampire A Lusth@Idle.fbx", true);
        ConfigureClipLoop($"{CharacterModelsFolder}/Vampire A Lusth@Walking.fbx", true);
        ConfigureClipLoop($"{CharacterModelsFolder}/Vampire A Lusth@Vampiric Bite.fbx", false);

        AnimationClip remyIdle = DuplicateClip($"{CharacterModelsFolder}/Remy@Idle.fbx", $"{ClipsFolder}/Remy_Idle.anim");
        AnimationClip remyRun = DuplicateClip($"{CharacterModelsFolder}/Remy@Running.fbx", $"{ClipsFolder}/Remy_Run.anim");
        AnimationClip girlIdle = DuplicateClip($"{CharacterModelsFolder}/Peasant Girl@Praying.fbx", $"{ClipsFolder}/PeasantGirl_Idle.anim");
        AnimationClip girlRun = DuplicateClip($"{CharacterModelsFolder}/Peasant Girl@Running.fbx", $"{ClipsFolder}/PeasantGirl_Run.anim");
        AnimationClip vampireIdle = DuplicateClip($"{CharacterModelsFolder}/Vampire A Lusth@Idle.fbx", $"{ClipsFolder}/Vampire_Idle.anim");
        AnimationClip vampireWalk = DuplicateClip($"{CharacterModelsFolder}/Vampire A Lusth@Walking.fbx", $"{ClipsFolder}/Vampire_Walk.anim");
        AnimationClip vampireAttack = DuplicateClip($"{CharacterModelsFolder}/Vampire A Lusth@Vampiric Bite.fbx", $"{ClipsFolder}/Vampire_Attack.anim");

        CreateSurvivorController($"{ControllersFolder}/Remy.controller", remyIdle, remyRun);
        CreateSurvivorController($"{ControllersFolder}/PeasantGirl.controller", girlIdle, girlRun);
        CreateMonsterController($"{ControllersFolder}/VampireMonster.controller", vampireIdle, vampireWalk, vampireAttack);
    }

    private static void ConfigureClipLoop(string assetPath, bool loop)
    {
        ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
        {
            return;
        }

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        if (clips == null || clips.Length == 0)
        {
            clips = importer.clipAnimations;
        }

        if (clips == null || clips.Length == 0)
        {
            return;
        }

        for (int index = 0; index < clips.Length; index++)
        {
            clips[index].loopTime = loop;
            clips[index].loopPose = loop;
        }

        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    private static AnimationClip DuplicateClip(string modelPath, string assetPath)
    {
        DeleteAssetIfPresent(assetPath);

        AnimationClip sourceClip = AssetDatabase.LoadAllAssetsAtPath(modelPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));

        if (sourceClip == null)
        {
            throw new InvalidOperationException($"No animation clip found in {modelPath}");
        }

        AnimationClip clipAsset = UnityEngine.Object.Instantiate(sourceClip);
        clipAsset.name = Path.GetFileNameWithoutExtension(assetPath);
        AssetDatabase.CreateAsset(clipAsset, assetPath);
        EditorUtility.SetDirty(clipAsset);
        return clipAsset;
    }

    private static void CreateSurvivorController(string controllerPath, Motion idleMotion, Motion runMotion)
    {
        DeleteAssetIfPresent(controllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = stateMachine.AddState("Idle");
        AnimatorState runState = stateMachine.AddState("Run");
        stateMachine.defaultState = idleState;

        idleState.motion = idleMotion;
        runState.motion = runMotion;

        AnimatorStateTransition toRun = idleState.AddTransition(runState);
        toRun.hasExitTime = false;
        toRun.duration = 0.08f;
        toRun.AddCondition(AnimatorConditionMode.Greater, 0.12f, "Speed");

        AnimatorStateTransition toIdle = runState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.08f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.08f, "Speed");
    }

    private static void CreateMonsterController(string controllerPath, Motion idleMotion, Motion walkMotion, Motion attackMotion)
    {
        DeleteAssetIfPresent(controllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = stateMachine.AddState("Idle");
        AnimatorState walkState = stateMachine.AddState("Walk");
        AnimatorState attackState = stateMachine.AddState("Attack");
        stateMachine.defaultState = idleState;

        idleState.motion = idleMotion;
        walkState.motion = walkMotion;
        attackState.motion = attackMotion;

        AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0.08f;
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.12f, "Speed");

        AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0.08f;
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.08f, "Speed");

        AnimatorStateTransition anyToAttack = stateMachine.AddAnyStateTransition(attackState);
        anyToAttack.hasExitTime = false;
        anyToAttack.duration = 0.04f;
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

        AnimatorStateTransition attackToWalk = attackState.AddTransition(walkState);
        attackToWalk.hasExitTime = true;
        attackToWalk.exitTime = 0.88f;
        attackToWalk.duration = 0.06f;
        attackToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.88f;
        attackToIdle.duration = 0.06f;
        attackToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
    }

    private static void CreateEnvironmentPrefabs()
    {
        Material wall = CreateLitMaterial("HouseWall", new Color(0.35f, 0.28f, 0.22f), 0f, 0.14f);
        Material roof = CreateLitMaterial("Roof", new Color(0.13f, 0.14f, 0.19f), 0f, 0.16f);
        Material wood = CreateLitMaterial("Wood", new Color(0.26f, 0.18f, 0.1f), 0f, 0.12f);
        Material window = CreateEmissiveMaterial("WindowWarm", new Color(0.18f, 0.16f, 0.12f), new Color(1f, 0.62f, 0.2f) * 1.8f);
        Material trunk = CreateLitMaterial("TreeTrunk", new Color(0.16f, 0.09f, 0.04f), 0f, 0.1f);
        Material leafOrange = CreateLitMaterial("LeavesOrange", new Color(0.73f, 0.39f, 0.09f), 0f, 0.14f);
        Material leafRed = CreateLitMaterial("LeavesRed", new Color(0.53f, 0.16f, 0.12f), 0f, 0.14f);
        Material leafGold = CreateLitMaterial("LeavesGold", new Color(0.74f, 0.55f, 0.15f), 0f, 0.15f);
        Material metal = CreateLitMaterial("LampMetal", new Color(0.13f, 0.15f, 0.18f), 0.04f, 0.28f);
        Material lampGlow = CreateEmissiveMaterial("LampGlow", new Color(0.08f, 0.07f, 0.05f), new Color(1f, 0.74f, 0.32f) * 2.8f);
        Material stone = CreateLitMaterial("Stone", new Color(0.24f, 0.24f, 0.26f), 0f, 0.08f);

        CreateHousePrefab($"{EnvironmentPrefabFolder}/VillageHouse.prefab", wall, roof, wood, window);
        CreateLampPrefab($"{EnvironmentPrefabFolder}/StreetLamp.prefab", metal, lampGlow);
        CreateTreePrefab($"{EnvironmentPrefabFolder}/AutumnTree.prefab", trunk, new[] { leafOrange, leafRed, leafGold });
        CreateRockPrefab($"{EnvironmentPrefabFolder}/ForestRock.prefab", stone);
    }

    private static void CreateEffectPrefabs()
    {
        Material blood = CreateParticleMaterial("ParticleBlood", new Color(0.65f, 0.08f, 0.08f, 0.95f));
        Material smoke = CreateParticleMaterial("ParticleSmoke", new Color(0.68f, 0.71f, 0.76f, 0.35f));
        Material ember = CreateParticleMaterial("ParticleEmber", new Color(1f, 0.72f, 0.25f, 0.92f));
        Material leaf = CreateParticleMaterial("ParticleLeaf", new Color(0.86f, 0.48f, 0.14f, 0.88f));
        Material gold = CreateParticleMaterial("ParticleGold", new Color(1f, 0.9f, 0.32f, 0.95f));

        CreateBloodBurstPrefab($"{EffectsPrefabFolder}/BloodBurst.prefab", blood);
        CreateMistBurstPrefab($"{EffectsPrefabFolder}/SpawnMist.prefab", smoke);
        CreateImpactBurstPrefab($"{EffectsPrefabFolder}/ImpactBurst.prefab", ember);
        CreateMuzzleFlashPrefab($"{EffectsPrefabFolder}/MuzzleFlash.prefab", ember);
        CreateGoldBurstPrefab($"{EffectsPrefabFolder}/GoldBurst.prefab", gold);
        CreateAutumnDriftPrefab($"{EffectsPrefabFolder}/AutumnDrift.prefab", leaf);
        CreateGroundFogPrefab($"{EffectsPrefabFolder}/GroundFog.prefab", smoke);
    }

    private static void CreateGameplayPrefabs()
    {
        Material goldBody = CreateEmissiveMaterial("GoldBody", new Color(0.78f, 0.63f, 0.14f), new Color(1f, 0.82f, 0.32f) * 1.8f);
        Material tracer = CreateUnlitColorMaterial("TracerLine", new Color(1f, 0.88f, 0.4f, 0.95f));

        CreateGoldPickupPrefab($"{GameplayPrefabFolder}/GoldPickup.prefab", goldBody);
        CreateShotTracerPrefab($"{GameplayPrefabFolder}/ShotTracer.prefab", tracer);
    }

    private static void CreateCharacterPrefabs()
    {
        Material skinWarm = CreateLitMaterial("HeroSkin", new Color(0.91f, 0.74f, 0.61f), 0f, 0.28f);
        Material remyCloth = CreateLitMaterial("RemyCloth", new Color(0.18f, 0.34f, 0.41f), 0f, 0.16f);
        Material remyPants = CreateLitMaterial("RemyPants", new Color(0.13f, 0.13f, 0.15f), 0f, 0.18f);
        Material remyMetal = CreateLitMaterial("RemyMetal", new Color(0.43f, 0.45f, 0.49f), 0.2f, 0.42f);

        Material girlSkin = CreateLitMaterial("GirlSkin", new Color(0.93f, 0.75f, 0.67f), 0f, 0.24f);
        Material girlDress = CreateLitMaterial("GirlDress", new Color(0.59f, 0.22f, 0.17f), 0f, 0.17f);
        Material girlApron = CreateLitMaterial("GirlApron", new Color(0.77f, 0.69f, 0.56f), 0f, 0.1f);
        Material girlHair = CreateLitMaterial("GirlHair", new Color(0.23f, 0.12f, 0.05f), 0f, 0.14f);

        Material vampireSkin = CreateLitMaterial("VampireSkin", new Color(0.76f, 0.77f, 0.86f), 0f, 0.36f);
        Material vampireCoat = CreateLitMaterial("VampireCoat", new Color(0.09f, 0.09f, 0.11f), 0f, 0.22f);
        Material vampireAccent = CreateLitMaterial("VampireAccent", new Color(0.45f, 0.07f, 0.08f), 0f, 0.2f);
        Material vampireDetail = CreateLitMaterial("VampireDetail", new Color(0.28f, 0.26f, 0.32f), 0f, 0.16f);

        CreateSurvivorPrefab(
            $"{CharacterModelsFolder}/Remy@Idle.fbx",
            $"{CharacterPrefabFolder}/RemySurvivor.prefab",
            $"{ControllersFolder}/Remy.controller",
            "Remy",
            1.88f,
            0.34f,
            new[] { skinWarm, remyCloth, remyPants, remyMetal });

        CreateSurvivorPrefab(
            $"{CharacterModelsFolder}/Peasant Girl.fbx",
            $"{CharacterPrefabFolder}/PeasantGirlSurvivor.prefab",
            $"{ControllersFolder}/PeasantGirl.controller",
            "Peasant Girl",
            1.72f,
            0.31f,
            new[] { girlSkin, girlDress, girlApron, girlHair });

        CreateMonsterPrefab(
            $"{CharacterModelsFolder}/Vampire A Lusth@Idle.fbx",
            $"{CharacterPrefabFolder}/VampireMonster.prefab",
            $"{ControllersFolder}/VampireMonster.controller",
            new[] { vampireSkin, vampireCoat, vampireAccent, vampireDetail });
    }

    private static void CreateSurvivorPrefab(
        string modelPath,
        string prefabPath,
        string controllerPath,
        string survivorName,
        float height,
        float radius,
        Material[] palette)
    {
        DeleteAssetIfPresent(prefabPath);

        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        GameObject root = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));
        GameObject modelInstance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
        modelInstance.name = "Visual";
        modelInstance.transform.SetParent(root.transform, false);

        Animator animator = modelInstance.GetComponent<Animator>() ?? modelInstance.AddComponent<Animator>();
        animator.avatar = LoadAvatar(modelPath);
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        animator.applyRootMotion = false;

        ApplyCharacterLook(modelInstance, palette);

        UnityEngine.AI.NavMeshAgent agent = root.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.speed = 5.1f;
        agent.acceleration = 25f;
        agent.angularSpeed = 720f;
        agent.radius = radius;
        agent.height = height;
        agent.stoppingDistance = 0.08f;

        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.height = height;
        collider.radius = radius;
        collider.center = new Vector3(0f, height * 0.5f, 0f);

        Rigidbody body = root.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        SurvivorAgent survivor = root.AddComponent<SurvivorAgent>();
        SerializedObject serializedSurvivor = new SerializedObject(survivor);
        serializedSurvivor.FindProperty("survivorName").stringValue = survivorName;
        serializedSurvivor.FindProperty("moveSpeed").floatValue = 5.2f;
        serializedSurvivor.FindProperty("panicBoostMultiplier").floatValue = 1.32f;
        serializedSurvivor.FindProperty("panicDistance").floatValue = 11.5f;
        serializedSurvivor.FindProperty("rotationSharpness").floatValue = 10f;
        serializedSurvivor.FindProperty("fireCooldown").floatValue = 0.34f;
        serializedSurvivor.FindProperty("fireRange").floatValue = 30f;
        serializedSurvivor.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateMonsterPrefab(string modelPath, string prefabPath, string controllerPath, Material[] palette)
    {
        DeleteAssetIfPresent(prefabPath);

        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        GameObject root = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));
        GameObject modelInstance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
        modelInstance.name = "Visual";
        modelInstance.transform.SetParent(root.transform, false);

        Animator animator = modelInstance.GetComponent<Animator>() ?? modelInstance.AddComponent<Animator>();
        animator.avatar = LoadAvatar(modelPath);
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        animator.applyRootMotion = false;

        ApplyCharacterLook(modelInstance, palette);

        UnityEngine.AI.NavMeshAgent agent = root.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.speed = 6.2f;
        agent.acceleration = 28f;
        agent.angularSpeed = 520f;
        agent.radius = 0.42f;
        agent.height = 1.95f;
        agent.stoppingDistance = 0.35f;

        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.height = 1.95f;
        collider.radius = 0.42f;
        collider.center = new Vector3(0f, 0.97f, 0f);

        Rigidbody body = root.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        MonsterAgent monster = root.AddComponent<MonsterAgent>();
        SerializedObject serializedMonster = new SerializedObject(monster);
        serializedMonster.FindProperty("maxHealth").intValue = 8;
        serializedMonster.FindProperty("roamSpeed").floatValue = 4.1f;
        serializedMonster.FindProperty("chaseSpeed").floatValue = 6.3f;
        serializedMonster.FindProperty("detectionRange").floatValue = 70f;
        serializedMonster.FindProperty("attackRange").floatValue = 2.35f;
        serializedMonster.FindProperty("attackCooldown").floatValue = 1.55f;
        serializedMonster.FindProperty("attackWindup").floatValue = 0.28f;
        serializedMonster.FindProperty("attackRecovery").floatValue = 0.65f;
        serializedMonster.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void ApplyCharacterLook(GameObject modelRoot, Material[] palette)
    {
        foreach (Renderer renderer in modelRoot.GetComponentsInChildren<Renderer>(true))
        {
            int slotCount = ResolveMaterialSlotCount(renderer);
            Material[] assignedMaterials = new Material[slotCount];
            for (int index = 0; index < slotCount; index++)
            {
                assignedMaterials[index] = palette[Mathf.Min(index, palette.Length - 1)];
            }

            renderer.sharedMaterials = assignedMaterials;
            renderer.receiveShadows = true;

            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                skinnedMeshRenderer.updateWhenOffscreen = true;
            }
        }
    }

    private static int ResolveMaterialSlotCount(Renderer renderer)
    {
        int slotCount = renderer.sharedMaterials != null ? renderer.sharedMaterials.Length : 0;

        if (renderer is SkinnedMeshRenderer skinnedMeshRenderer && skinnedMeshRenderer.sharedMesh != null)
        {
            slotCount = Mathf.Max(slotCount, skinnedMeshRenderer.sharedMesh.subMeshCount);
        }

        if (renderer.TryGetComponent(out MeshFilter meshFilter) && meshFilter.sharedMesh != null)
        {
            slotCount = Mathf.Max(slotCount, meshFilter.sharedMesh.subMeshCount);
        }

        return Mathf.Max(1, slotCount);
    }

    private static void BuildSceneContent()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            UnityEngine.Object.DestroyImmediate(rootObject);
        }

        Material ground = CreateLitMaterial("Ground", new Color(0.12f, 0.13f, 0.1f), 0f, 0.08f);
        Material forest = CreateLitMaterial("ForestFloor", new Color(0.11f, 0.14f, 0.09f), 0f, 0.06f);
        Material road = CreateLitMaterial("Road", new Color(0.11f, 0.12f, 0.14f), 0f, 0.09f);
        Material curb = CreateLitMaterial("Curb", new Color(0.2f, 0.18f, 0.15f), 0f, 0.08f);
        Material fence = CreateLitMaterial("Fence", new Color(0.18f, 0.12f, 0.07f), 0f, 0.1f);
        Material stone = CreateLitMaterial("GateStone", new Color(0.25f, 0.25f, 0.27f), 0f, 0.08f);
        Material plaza = CreateLitMaterial("Plaza", new Color(0.16f, 0.14f, 0.13f), 0f, 0.06f);

        GameObject environmentRoot = new GameObject("Environment");

        GameObject groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        groundPlane.name = "Ground";
        groundPlane.transform.SetParent(environmentRoot.transform);
        groundPlane.transform.position = Vector3.zero;
        groundPlane.transform.localScale = new Vector3(24f, 1f, 24f);
        groundPlane.GetComponent<Renderer>().sharedMaterial = ground;

        GameObject forestPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        forestPlane.name = "ForestBlend";
        forestPlane.transform.SetParent(environmentRoot.transform);
        forestPlane.transform.position = new Vector3(0f, -0.02f, 0f);
        forestPlane.transform.localScale = new Vector3(26f, 1f, 26f);
        forestPlane.GetComponent<Renderer>().sharedMaterial = forest;

        CreateRoad(environmentRoot.transform, "MainBoulevard", new Vector3(0f, 0.04f, 0f), new Vector3(12f, 0.14f, 150f), road);
        CreateRoad(environmentRoot.transform, "CrossRoad", new Vector3(0f, 0.045f, -2f), new Vector3(90f, 0.14f, 10f), road);
        CreateRoad(environmentRoot.transform, "ForestRoadEast", new Vector3(28f, 0.04f, 18f), new Vector3(28f, 0.14f, 8f), road);
        CreateRoad(environmentRoot.transform, "ForestRoadWest", new Vector3(-28f, 0.04f, 18f), new Vector3(28f, 0.14f, 8f), road);
        CreateRoad(environmentRoot.transform, "VillagePlaza", new Vector3(0f, 0.05f, 20f), new Vector3(24f, 0.16f, 16f), plaza);

        CreateRoad(environmentRoot.transform, "LeftCurb", new Vector3(-6.2f, 0.08f, 0f), new Vector3(0.25f, 0.22f, 150f), curb);
        CreateRoad(environmentRoot.transform, "RightCurb", new Vector3(6.2f, 0.08f, 0f), new Vector3(0.25f, 0.22f, 150f), curb);

        PlaceVillage(environmentRoot.transform);
        PlaceForest(environmentRoot.transform);
        PlaceStreetLights(environmentRoot.transform);
        PlaceFences(environmentRoot.transform, fence);
        CreateNorthernGate(environmentRoot.transform, stone);
        PlaceAtmosphereEffects(environmentRoot.transform);

        GameObject lightingRoot = new GameObject("Lighting");
        CreateMoonlight(lightingRoot.transform);

        Camera camera = CreateCameraRig();
        GameHud hud = CreateHud();
        CreateEventSystem();
        GameObject spawnRoot = new GameObject("ScenarioPoints");

        Transform remySpawn = CreateMarker(spawnRoot.transform, "RemySpawn", new Vector3(-1.8f, 0f, -62f), new Vector3(0f, 0f, 0f)).transform;
        Transform girlSpawn = CreateMarker(spawnRoot.transform, "GirlSpawn", new Vector3(1.7f, 0f, -58f), new Vector3(0f, 0f, 0f)).transform;
        Transform monsterSpawn = CreateMarker(spawnRoot.transform, "MonsterSpawn", new Vector3(0f, 0f, 68f), new Vector3(0f, 180f, 0f)).transform;

        Vector3[] goldPositions =
        {
            new Vector3(-10f, 0.3f, -42f),
            new Vector3(10f, 0.3f, -34f),
            new Vector3(-24f, 0.3f, -12f),
            new Vector3(22f, 0.3f, -6f),
            new Vector3(-16f, 0.3f, 10f),
            new Vector3(16f, 0.3f, 14f),
            new Vector3(0f, 0.3f, 24f),
            new Vector3(-32f, 0.3f, 26f),
            new Vector3(30f, 0.3f, 32f),
            new Vector3(-38f, 0.3f, 48f),
            new Vector3(38f, 0.3f, 50f),
            new Vector3(0f, 0.3f, 56f)
        };

        Transform[] goldSpawnPoints = new Transform[goldPositions.Length];
        for (int index = 0; index < goldPositions.Length; index++)
        {
            goldSpawnPoints[index] = CreateMarker(
                spawnRoot.transform,
                $"GoldSpawn_{index:00}",
                goldPositions[index],
                Vector3.zero).transform;
        }

        GameObject directorObject = new GameObject("NocturneDirector");
        NocturneScenarioDirector director = directorObject.AddComponent<NocturneScenarioDirector>();
        SerializedObject serializedDirector = new SerializedObject(director);

        GameObject[] survivorPrefabs =
        {
            AssetDatabase.LoadAssetAtPath<GameObject>($"{CharacterPrefabFolder}/RemySurvivor.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>($"{CharacterPrefabFolder}/PeasantGirlSurvivor.prefab")
        };

        serializedDirector.FindProperty("survivorPrefabs").arraySize = survivorPrefabs.Length;
        serializedDirector.FindProperty("survivorSpawnPoints").arraySize = 2;

        for (int index = 0; index < survivorPrefabs.Length; index++)
        {
            serializedDirector.FindProperty("survivorPrefabs").GetArrayElementAtIndex(index).objectReferenceValue = survivorPrefabs[index];
        }

        serializedDirector.FindProperty("survivorSpawnPoints").GetArrayElementAtIndex(0).objectReferenceValue = remySpawn;
        serializedDirector.FindProperty("survivorSpawnPoints").GetArrayElementAtIndex(1).objectReferenceValue = girlSpawn;
        serializedDirector.FindProperty("monsterPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>($"{CharacterPrefabFolder}/VampireMonster.prefab");
        serializedDirector.FindProperty("monsterSpawnPoint").objectReferenceValue = monsterSpawn;
        serializedDirector.FindProperty("goldPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>($"{GameplayPrefabFolder}/GoldPickup.prefab");
        serializedDirector.FindProperty("goldSpawnPoints").arraySize = goldSpawnPoints.Length;

        for (int index = 0; index < goldSpawnPoints.Length; index++)
        {
            serializedDirector.FindProperty("goldSpawnPoints").GetArrayElementAtIndex(index).objectReferenceValue = goldSpawnPoints[index];
        }

        serializedDirector.FindProperty("cameraRig").objectReferenceValue = camera.GetComponent<CameraRigFollow>();
        serializedDirector.FindProperty("hud").objectReferenceValue = hud;
        serializedDirector.FindProperty("spawnEffectPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<ParticleSystem>($"{EffectsPrefabFolder}/SpawnMist.prefab");
        serializedDirector.FindProperty("bloodBurstPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<ParticleSystem>($"{EffectsPrefabFolder}/BloodBurst.prefab");
        serializedDirector.FindProperty("muzzleFlashPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<ParticleSystem>($"{EffectsPrefabFolder}/MuzzleFlash.prefab");
        serializedDirector.FindProperty("impactBurstPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<ParticleSystem>($"{EffectsPrefabFolder}/ImpactBurst.prefab");
        serializedDirector.FindProperty("goldCollectBurstPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<ParticleSystem>($"{EffectsPrefabFolder}/GoldBurst.prefab");
        serializedDirector.FindProperty("shotTracerPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>($"{GameplayPrefabFolder}/ShotTracer.prefab");
        serializedDirector.FindProperty("goldScoreValue").intValue = 10;
        serializedDirector.FindProperty("weaponUnlockScore").intValue = 50;
        serializedDirector.ApplyModifiedPropertiesWithoutUndo();

        ConfigureRenderSettings();

#pragma warning disable 618
        SetStaticRecursive(environmentRoot, StaticEditorFlags.NavigationStatic | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);
        SetStaticRecursive(groundPlane, StaticEditorFlags.NavigationStatic | StaticEditorFlags.BatchingStatic);
        SetStaticRecursive(forestPlane, StaticEditorFlags.NavigationStatic | StaticEditorFlags.BatchingStatic);
#pragma warning restore 618

        EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        EditorSceneManager.SaveScene(scene);
    }

    private static void CreateRoad(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = name;
        road.transform.SetParent(parent);
        road.transform.position = position;
        road.transform.localScale = scale;
        road.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void PlaceVillage(Transform parent)
    {
        GameObject housePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnvironmentPrefabFolder}/VillageHouse.prefab");
        GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnvironmentPrefabFolder}/AutumnTree.prefab");
        GameObject rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnvironmentPrefabFolder}/ForestRock.prefab");

        Vector3[] housePositions =
        {
            new Vector3(-18f, 0f, -44f), new Vector3(18f, 0f, -42f),
            new Vector3(-19f, 0f, -24f), new Vector3(19f, 0f, -20f),
            new Vector3(-18f, 0f, -2f), new Vector3(18f, 0f, 2f),
            new Vector3(-20f, 0f, 22f), new Vector3(20f, 0f, 22f),
            new Vector3(-26f, 0f, 10f), new Vector3(26f, 0f, 10f)
        };

        for (int index = 0; index < housePositions.Length; index++)
        {
            GameObject house = PrefabUtility.InstantiatePrefab(housePrefab) as GameObject;
            house.transform.SetParent(parent);
            house.transform.position = housePositions[index];
            house.transform.rotation = Quaternion.Euler(0f, housePositions[index].x < 0f ? 86f : -86f, 0f);
            house.transform.localScale = Vector3.one * (0.95f + (index % 3) * 0.08f);
        }

        Vector3[] townTrees =
        {
            new Vector3(-32f, 0f, -34f),
            new Vector3(31f, 0f, -30f),
            new Vector3(-31f, 0f, -8f),
            new Vector3(30f, 0f, -10f),
            new Vector3(-29f, 0f, 18f),
            new Vector3(31f, 0f, 16f),
            new Vector3(-12f, 0f, 34f),
            new Vector3(13f, 0f, 36f)
        };

        for (int index = 0; index < townTrees.Length; index++)
        {
            GameObject tree = PrefabUtility.InstantiatePrefab(treePrefab) as GameObject;
            tree.transform.SetParent(parent);
            tree.transform.position = townTrees[index];
            tree.transform.rotation = Quaternion.Euler(0f, index * 35f, 0f);
            tree.transform.localScale = Vector3.one * (0.92f + (index % 2) * 0.18f);
        }

        Vector3[] rockPositions =
        {
            new Vector3(-36f, 0f, -20f),
            new Vector3(36f, 0f, -16f),
            new Vector3(-34f, 0f, 8f),
            new Vector3(35f, 0f, 30f)
        };

        for (int index = 0; index < rockPositions.Length; index++)
        {
            GameObject rock = PrefabUtility.InstantiatePrefab(rockPrefab) as GameObject;
            rock.transform.SetParent(parent);
            rock.transform.position = rockPositions[index];
            rock.transform.rotation = Quaternion.Euler(0f, index * 28f, 0f);
            rock.transform.localScale = Vector3.one * (0.85f + index * 0.07f);
        }
    }

    private static void PlaceForest(Transform parent)
    {
        GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnvironmentPrefabFolder}/AutumnTree.prefab");
        GameObject rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnvironmentPrefabFolder}/ForestRock.prefab");

        for (int x = -70; x <= 70; x += 10)
        {
            for (int z = -70; z <= 80; z += 10)
            {
                bool isMainRoad = Mathf.Abs(x) < 10 && z > -75 && z < 78;
                bool isCrossRoad = Mathf.Abs(z + 2) < 10 && Mathf.Abs(x) < 48;
                bool isTownCore = Mathf.Abs(x) < 34 && z > -48 && z < 40;
                bool isEastForestRoad = x > 12 && x < 44 && Mathf.Abs(z - 18) < 10;
                bool isWestForestRoad = x < -12 && x > -44 && Mathf.Abs(z - 18) < 10;

                if (isMainRoad || isCrossRoad || isTownCore || isEastForestRoad || isWestForestRoad)
                {
                    continue;
                }

                GameObject tree = PrefabUtility.InstantiatePrefab(treePrefab) as GameObject;
                tree.transform.SetParent(parent);
                tree.transform.position = new Vector3(
                    x + (((z / 10) % 2 == 0) ? 1.8f : -1.8f),
                    0f,
                    z + (((x / 10) % 3 == 0) ? 1.2f : -1.4f));
                tree.transform.rotation = Quaternion.Euler(0f, (x * 7 + z * 5) % 360, 0f);
                tree.transform.localScale = Vector3.one * (0.8f + Mathf.Abs((x + z) % 4) * 0.08f);

                if ((x + z) % 20 == 0)
                {
                    GameObject rock = PrefabUtility.InstantiatePrefab(rockPrefab) as GameObject;
                    rock.transform.SetParent(parent);
                    rock.transform.position = tree.transform.position + new Vector3(1.8f, 0f, -1.5f);
                    rock.transform.rotation = Quaternion.Euler(0f, (x * 13 + z * 9) % 360, 0f);
                    rock.transform.localScale = Vector3.one * 0.72f;
                }
            }
        }
    }

    private static void PlaceStreetLights(Transform parent)
    {
        GameObject lampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnvironmentPrefabFolder}/StreetLamp.prefab");

        for (float z = -56f; z <= 56f; z += 14f)
        {
            GameObject leftLamp = PrefabUtility.InstantiatePrefab(lampPrefab) as GameObject;
            leftLamp.transform.SetParent(parent);
            leftLamp.transform.position = new Vector3(-8.8f, 0f, z);

            GameObject rightLamp = PrefabUtility.InstantiatePrefab(lampPrefab) as GameObject;
            rightLamp.transform.SetParent(parent);
            rightLamp.transform.position = new Vector3(8.8f, 0f, z + 6f);
        }

        for (float x = -36f; x <= 36f; x += 18f)
        {
            GameObject lamp = PrefabUtility.InstantiatePrefab(lampPrefab) as GameObject;
            lamp.transform.SetParent(parent);
            lamp.transform.position = new Vector3(x, 0f, -12f);
            lamp.transform.rotation = Quaternion.Euler(0f, x < 0f ? 90f : -90f, 0f);
        }
    }

    private static void PlaceFences(Transform parent, Material fenceMaterial)
    {
        GameObject fenceRoot = new GameObject("BrokenFences");
        fenceRoot.transform.SetParent(parent);

        for (int side = -1; side <= 1; side += 2)
        {
            for (int index = 0; index < 10; index++)
            {
                float zPosition = -54f + index * 8f;
                if (index == 4 || index == 5)
                {
                    continue;
                }

                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = $"FencePost_{side}_{index}";
                post.transform.SetParent(fenceRoot.transform);
                post.transform.position = new Vector3(14f * side, 0.75f, zPosition);
                post.transform.localScale = new Vector3(0.18f, 1.5f, 0.18f);
                post.GetComponent<Renderer>().sharedMaterial = fenceMaterial;

                GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = $"FenceRail_{side}_{index}";
                rail.transform.SetParent(fenceRoot.transform);
                rail.transform.position = new Vector3(14f * side, 1.12f, zPosition + 3.4f);
                rail.transform.localScale = new Vector3(0.12f, 0.14f, 6.8f);
                rail.GetComponent<Renderer>().sharedMaterial = fenceMaterial;
            }
        }
    }

    private static void CreateNorthernGate(Transform parent, Material stoneMaterial)
    {
        GameObject gateRoot = new GameObject("ForestArch");
        gateRoot.transform.SetParent(parent);

        CreatePrimitivePart(gateRoot.transform, PrimitiveType.Cube, "LeftPillar", new Vector3(-4.2f, 2.3f, 48f), Vector3.zero, new Vector3(1.4f, 4.6f, 1.4f), stoneMaterial, true);
        CreatePrimitivePart(gateRoot.transform, PrimitiveType.Cube, "RightPillar", new Vector3(4.2f, 2.3f, 48f), Vector3.zero, new Vector3(1.4f, 4.6f, 1.4f), stoneMaterial, true);
        CreatePrimitivePart(gateRoot.transform, PrimitiveType.Cube, "TopBeam", new Vector3(0f, 4.9f, 48f), Vector3.zero, new Vector3(10.5f, 0.9f, 1.4f), stoneMaterial, true);
    }

    private static void PlaceAtmosphereEffects(Transform parent)
    {
        GameObject driftPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{EffectsPrefabFolder}/AutumnDrift.prefab");
        GameObject fogPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{EffectsPrefabFolder}/GroundFog.prefab");

        Vector3[] driftPositions =
        {
            new Vector3(-54f, 7f, -18f),
            new Vector3(54f, 7f, -14f),
            new Vector3(-48f, 7f, 42f),
            new Vector3(48f, 7f, 40f),
            new Vector3(0f, 7f, 74f)
        };

        foreach (Vector3 position in driftPositions)
        {
            GameObject drift = PrefabUtility.InstantiatePrefab(driftPrefab) as GameObject;
            drift.transform.SetParent(parent);
            drift.transform.position = position;
        }

        Vector3[] fogPositions =
        {
            new Vector3(0f, 0.1f, -40f),
            new Vector3(-18f, 0.1f, -2f),
            new Vector3(18f, 0.1f, 8f),
            new Vector3(0f, 0.1f, 34f),
            new Vector3(0f, 0.1f, 62f)
        };

        foreach (Vector3 position in fogPositions)
        {
            GameObject fog = PrefabUtility.InstantiatePrefab(fogPrefab) as GameObject;
            fog.transform.SetParent(parent);
            fog.transform.position = position;
        }
    }

    private static Camera CreateCameraRig()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
        camera.fieldOfView = 56f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 320f;
        camera.transform.position = new Vector3(0f, 18f, -22f);
        camera.transform.rotation = Quaternion.Euler(30f, 0f, 0f);

        cameraObject.AddComponent<AudioListener>();
        CameraRigFollow follow = cameraObject.AddComponent<CameraRigFollow>();
        SerializedObject serializedFollow = new SerializedObject(follow);
        serializedFollow.FindProperty("baseOffset").vector3Value = new Vector3(0f, 16f, -22f);
        serializedFollow.FindProperty("followSharpness").floatValue = 4f;
        serializedFollow.FindProperty("rotationSharpness").floatValue = 4.5f;
        serializedFollow.FindProperty("distanceScale").floatValue = 0.34f;
        serializedFollow.ApplyModifiedPropertiesWithoutUndo();

        return camera;
    }

    private static GameHud CreateHud()
    {
        GameObject canvasObject = new GameObject("HUD");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject infoPanel = CreatePanel(
            canvas.transform,
            "InfoPanel",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(18f, -18f),
            new Vector2(390f, 335f),
            new Color(0f, 0f, 0f, 0.48f));

        Text mainText = CreateHudText(
            infoPanel.transform,
            "MainText",
            font,
            15,
            TextAnchor.UpperLeft,
            new Vector2(18f, -18f),
            new Vector2(350f, 300f),
            new Color(0.95f, 0.94f, 0.88f));

        GameObject statusPanel = CreatePanel(
            canvas.transform,
            "StatusPanel",
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -18f),
            new Vector2(720f, 68f),
            new Color(0f, 0f, 0f, 0.42f));

        Text statusText = CreateHudText(
            statusPanel.transform,
            "StatusText",
            font,
            18,
            TextAnchor.UpperCenter,
            new Vector2(0f, -12f),
            new Vector2(660f, 42f),
            new Color(1f, 0.85f, 0.45f));

        GameObject startPanel = CreatePanel(
            canvas.transform,
            "StartPanel",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(640f, 360f),
            new Color(0f, 0f, 0f, 0.62f));

        Text titleText = CreateHudText(
            startPanel.transform,
            "StartTitle",
            font,
            34,
            TextAnchor.UpperCenter,
            new Vector2(0f, -26f),
            new Vector2(560f, 54f),
            new Color(0.96f, 0.94f, 0.88f));
        titleText.text = "Nocturne Blood Run";

        Text startMessage = CreateHudText(
            startPanel.transform,
            "StartMessage",
            font,
            18,
            TextAnchor.UpperCenter,
            new Vector2(0f, -96f),
            new Vector2(540f, 120f),
            new Color(0.86f, 0.9f, 0.98f));

        Button startButton = CreateButton(
            startPanel.transform,
            "StartButton",
            font,
            "Baslat",
            new Vector2(0f, -210f),
            new Vector2(200f, 54f),
            new Color(0.22f, 0.47f, 0.28f, 0.92f),
            new Color(0.97f, 0.98f, 0.94f));

        Button restartButton = CreateButton(
            canvas.transform,
            "RestartButton",
            font,
            "Yeniden Baslat",
            new Vector2(-22f, -22f),
            new Vector2(190f, 48f),
            new Color(0.48f, 0.18f, 0.16f, 0.92f),
            new Color(0.98f, 0.96f, 0.94f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f));

        GameHud hud = canvasObject.AddComponent<GameHud>();
        hud.Configure(mainText, statusText, startButton, restartButton, startPanel, infoPanel, statusPanel);
        return hud;
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static Text CreateHudText(
        Transform parent,
        string name,
        Font font,
        int fontSize,
        TextAnchor alignment,
        Vector2 anchoredPosition,
        Vector2 size,
        Color color)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = alignment == TextAnchor.UpperLeft ? new Vector2(0f, 1f) : new Vector2(0.5f, 1f);
        rectTransform.anchorMax = rectTransform.anchorMin;
        rectTransform.pivot = rectTransform.anchorMin;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.text = string.Empty;
        return text;
    }

    private static GameObject CreatePanel(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size,
        Color color)
    {
        GameObject panelObject = new GameObject(name);
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image image = panelObject.AddComponent<Image>();
        image.color = color;
        return panelObject;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        Font font,
        string label,
        Vector2 anchoredPosition,
        Vector2 size,
        Color buttonColor,
        Color textColor)
    {
        return CreateButton(
            parent,
            name,
            font,
            label,
            anchoredPosition,
            size,
            buttonColor,
            textColor,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f));
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        Font font,
        string label,
        Vector2 anchoredPosition,
        Vector2 size,
        Color buttonColor,
        Color textColor,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = buttonColor;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonColor * 1.1f;
        colors.pressedColor = buttonColor * 0.9f;
        colors.selectedColor = buttonColor * 1.05f;
        button.colors = colors;

        Text labelText = CreateHudText(
            buttonObject.transform,
            "Label",
            font,
            20,
            TextAnchor.MiddleCenter,
            Vector2.zero,
            size,
            textColor);
        labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
        labelText.text = label;
        return button;
    }

    private static void CreateMoonlight(Transform parent)
    {
        GameObject lightObject = new GameObject("Directional Light");
        lightObject.transform.SetParent(parent);
        lightObject.transform.rotation = Quaternion.Euler(31f, -36f, 0f);

        Light moonlight = lightObject.AddComponent<Light>();
        moonlight.type = LightType.Directional;
        moonlight.intensity = 1.18f;
        moonlight.color = new Color(0.56f, 0.6f, 0.74f);
        moonlight.shadows = LightShadows.Soft;

        GameObject fillObject = new GameObject("VillageFill");
        fillObject.transform.SetParent(parent);
        fillObject.transform.rotation = Quaternion.Euler(52f, 122f, 0f);

        Light fillLight = fillObject.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.intensity = 0.42f;
        fillLight.color = new Color(0.42f, 0.39f, 0.33f);
    }

    private static void ConfigureRenderSettings()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.13f, 0.14f, 0.16f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.1f, 0.11f, 0.13f);
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.skybox = null;
        RenderSettings.subtractiveShadowColor = new Color(0.14f, 0.11f, 0.1f);
    }

    private static Material CreateLitMaterial(string name, Color color, float metallic, float smoothness)
    {
        string assetPath = $"{MaterialsFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        Shader shader = ResolveLitShader();

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, assetPath);
        }
        else
        {
            material.shader = shader;
        }

        SetMaterialColor(material, color);
        SetMaterialFloat(material, "_Metallic", metallic);
        SetMaterialFloat(material, "_Glossiness", smoothness);
        SetMaterialFloat(material, "_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateEmissiveMaterial(string name, Color color, Color emission)
    {
        Material material = CreateLitMaterial(name, color, 0f, 0.2f);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateParticleMaterial(string name, Color color)
    {
        string assetPath = $"{MaterialsFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        Shader shader = ResolveParticleShader();
        Texture2D particleTexture = LoadOrCreateSoftParticleTexture();

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, assetPath);
        }
        else
        {
            material.shader = shader;
        }

        SetMaterialColor(material, color);
        AssignTexture(material, "_BaseMap", particleTexture);
        AssignTexture(material, "_MainTex", particleTexture);
        material.mainTexture = particleTexture;

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
        }

        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 2f);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat("_ZWrite", 0f);
        }

        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = 3000;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D LoadOrCreateSoftParticleTexture()
    {
        string assetPath = $"{MaterialsFolder}/ParticleSoftCircle.asset";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture != null)
        {
            return texture;
        }

        const int size = 64;
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "ParticleSoftCircle",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        float half = (size - 1) * 0.5f;
        float radius = half * 0.9f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float offsetX = x - half;
                float offsetY = y - half;
                float distance = Mathf.Sqrt(offsetX * offsetX + offsetY * offsetY);
                float alpha = Mathf.Clamp01(1f - Mathf.InverseLerp(radius * 0.2f, radius, distance));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
        }

        texture.Apply();
        AssetDatabase.CreateAsset(texture, assetPath);
        EditorUtility.SetDirty(texture);
        return texture;
    }

    private static void AssignTexture(Material material, string propertyName, Texture texture)
    {
        if (texture != null && material.HasProperty(propertyName))
        {
            material.SetTexture(propertyName, texture);
        }
    }

    private static Material CreateUnlitColorMaterial(string name, Color color)
    {
        string assetPath = $"{MaterialsFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        Shader shader = ResolveUnlitShader();

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, assetPath);
        }
        else
        {
            material.shader = shader;
        }

        SetMaterialColor(material, color);
        material.renderQueue = 3000;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Shader ResolveLitShader()
    {
        if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
        {
            return Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        }

        return Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
    }

    private static Shader ResolveParticleShader()
    {
        if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
        {
            return Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default");
        }

        return Shader.Find("Particles/Standard Unlit")
            ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended")
            ?? Shader.Find("Sprites/Default");
    }

    private static Shader ResolveUnlitShader()
    {
        if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
        {
            return Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        }

        return Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private static void SetMaterialFloat(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private static void CreateHousePrefab(string prefabPath, Material wall, Material roof, Material wood, Material window)
    {
        DeleteAssetIfPresent(prefabPath);

        GameObject root = new GameObject("VillageHouse");
        CreatePrimitivePart(root.transform, PrimitiveType.Cube, "Body", new Vector3(0f, 2.2f, 0f), Vector3.zero, new Vector3(7f, 4.4f, 6.8f), wall, true);
        CreatePrimitivePart(root.transform, PrimitiveType.Cube, "RoofLeft", new Vector3(-1.6f, 5.02f, 0f), new Vector3(0f, 0f, 32f), new Vector3(4f, 0.32f, 7.2f), roof, false);
        CreatePrimitivePart(root.transform, PrimitiveType.Cube, "RoofRight", new Vector3(1.6f, 5.02f, 0f), new Vector3(0f, 0f, -32f), new Vector3(4f, 0.32f, 7.2f), roof, false);
        CreatePrimitivePart(root.transform, PrimitiveType.Cube, "Door", new Vector3(0f, 1.25f, 3.42f), Vector3.zero, new Vector3(1.4f, 2.5f, 0.14f), wood, false);
        CreatePrimitivePart(root.transform, PrimitiveType.Cube, "WindowLeft", new Vector3(-2f, 2.4f, 3.45f), Vector3.zero, new Vector3(1.1f, 0.9f, 0.08f), window, false);
        CreatePrimitivePart(root.transform, PrimitiveType.Cube, "WindowRight", new Vector3(2f, 2.4f, 3.45f), Vector3.zero, new Vector3(1.1f, 0.9f, 0.08f), window, false);
        CreatePrimitivePart(root.transform, PrimitiveType.Cube, "Chimney", new Vector3(2.1f, 6.15f, -1.2f), Vector3.zero, new Vector3(0.82f, 2.1f, 0.82f), wall, true);

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateLampPrefab(string prefabPath, Material poleMaterial, Material glowMaterial)
    {
        DeleteAssetIfPresent(prefabPath);

        GameObject root = new GameObject("StreetLamp");
        CreatePrimitivePart(root.transform, PrimitiveType.Cylinder, "Pole", new Vector3(0f, 2.5f, 0f), Vector3.zero, new Vector3(0.18f, 2.5f, 0.18f), poleMaterial, true);
        CreatePrimitivePart(root.transform, PrimitiveType.Cube, "Arm", new Vector3(0.52f, 4.72f, 0f), Vector3.zero, new Vector3(0.92f, 0.1f, 0.1f), poleMaterial, false);
        CreatePrimitivePart(root.transform, PrimitiveType.Sphere, "Bulb", new Vector3(0.92f, 4.42f, 0f), Vector3.zero, new Vector3(0.28f, 0.28f, 0.28f), glowMaterial, false);

        GameObject lightObject = new GameObject("Glow");
        lightObject.transform.SetParent(root.transform, false);
        lightObject.transform.localPosition = new Vector3(0.92f, 4.42f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 10.5f;
        light.intensity = 3.4f;
        light.color = new Color(1f, 0.76f, 0.35f);
        light.shadows = LightShadows.Soft;

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateTreePrefab(string prefabPath, Material trunk, IReadOnlyList<Material> leaves)
    {
        DeleteAssetIfPresent(prefabPath);

        GameObject root = new GameObject("AutumnTree");
        CreatePrimitivePart(root.transform, PrimitiveType.Cylinder, "Trunk", new Vector3(0f, 2.4f, 0f), Vector3.zero, new Vector3(0.42f, 2.4f, 0.42f), trunk, true);
        CreatePrimitivePart(root.transform, PrimitiveType.Sphere, "CanopyCore", new Vector3(0f, 5.6f, 0f), Vector3.zero, new Vector3(3.6f, 2.8f, 3.6f), leaves[0], false);
        CreatePrimitivePart(root.transform, PrimitiveType.Sphere, "CanopyLeft", new Vector3(-1.28f, 5.2f, 0.9f), Vector3.zero, new Vector3(2.7f, 2.2f, 2.7f), leaves[1], false);
        CreatePrimitivePart(root.transform, PrimitiveType.Sphere, "CanopyRight", new Vector3(1.16f, 5.25f, -0.8f), Vector3.zero, new Vector3(2.5f, 2.1f, 2.5f), leaves[2], false);
        CreatePrimitivePart(root.transform, PrimitiveType.Sphere, "LeafPile", new Vector3(0f, 0.12f, 0f), Vector3.zero, new Vector3(2.6f, 0.22f, 2.6f), leaves[0], false);

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateRockPrefab(string prefabPath, Material rockMaterial)
    {
        DeleteAssetIfPresent(prefabPath);

        GameObject root = new GameObject("ForestRock");
        CreatePrimitivePart(root.transform, PrimitiveType.Sphere, "Rock", new Vector3(0f, 0.5f, 0f), new Vector3(0f, 0f, 0f), new Vector3(1.6f, 0.9f, 1.3f), rockMaterial, true);
        CreatePrimitivePart(root.transform, PrimitiveType.Sphere, "RockLump", new Vector3(0.45f, 0.7f, -0.18f), Vector3.zero, new Vector3(0.9f, 0.7f, 0.8f), rockMaterial, true);

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateGoldPickupPrefab(string prefabPath, Material goldMaterial)
    {
        DeleteAssetIfPresent(prefabPath);

        GameObject root = new GameObject("GoldPickup");
        CreatePrimitivePart(root.transform, PrimitiveType.Cylinder, "Coin", new Vector3(0f, 0.35f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.45f, 0.08f, 0.45f), goldMaterial, false);
        CreatePrimitivePart(root.transform, PrimitiveType.Sphere, "Glow", new Vector3(0f, 0.35f, 0f), Vector3.zero, new Vector3(0.18f, 0.18f, 0.18f), goldMaterial, false);

        SphereCollider trigger = root.AddComponent<SphereCollider>();
        trigger.radius = 0.65f;
        trigger.isTrigger = true;

        root.AddComponent<GoldCollectible>();

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateShotTracerPrefab(string prefabPath, Material tracerMaterial)
    {
        DeleteAssetIfPresent(prefabPath);

        GameObject root = new GameObject("ShotTracer");
        LineRenderer lineRenderer = root.AddComponent<LineRenderer>();
        lineRenderer.sharedMaterial = tracerMaterial;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = 0.12f;
        lineRenderer.endWidth = 0.02f;
        lineRenderer.positionCount = 2;
        lineRenderer.numCapVertices = 2;

        root.AddComponent<ShotTracer>();

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateBloodBurstPrefab(string prefabPath, Material material)
    {
        DeleteAssetIfPresent(prefabPath);
        GameObject root = new GameObject("BloodBurst");
        ParticleSystem system = root.AddComponent<ParticleSystem>();
        ConfigureBurstParticle(system, material, new Color(0.72f, 0.08f, 0.08f, 0.95f), 34, 0.95f, 1.1f, 3f, 0.14f, ParticleSystemShapeType.Sphere);
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateMistBurstPrefab(string prefabPath, Material material)
    {
        DeleteAssetIfPresent(prefabPath);
        GameObject root = new GameObject("SpawnMist");
        ParticleSystem system = root.AddComponent<ParticleSystem>();
        ConfigureBurstParticle(system, material, new Color(0.7f, 0.74f, 0.78f, 0.55f), 26, 1.4f, 1.2f, 1.7f, 0.3f, ParticleSystemShapeType.Cone);
        var shape = system.shape;
        shape.angle = 18f;
        shape.radius = 0.4f;
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateImpactBurstPrefab(string prefabPath, Material material)
    {
        DeleteAssetIfPresent(prefabPath);
        GameObject root = new GameObject("ImpactBurst");
        ParticleSystem system = root.AddComponent<ParticleSystem>();
        ConfigureBurstParticle(system, material, new Color(1f, 0.76f, 0.3f, 0.95f), 22, 0.65f, 0.55f, 2.4f, 0.1f, ParticleSystemShapeType.Sphere);
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateMuzzleFlashPrefab(string prefabPath, Material material)
    {
        DeleteAssetIfPresent(prefabPath);
        GameObject root = new GameObject("MuzzleFlash");
        ParticleSystem system = root.AddComponent<ParticleSystem>();
        ConfigureBurstParticle(system, material, new Color(1f, 0.84f, 0.38f, 0.95f), 12, 0.28f, 0.2f, 2f, 0.12f, ParticleSystemShapeType.Cone);
        var shape = system.shape;
        shape.angle = 12f;
        shape.radius = 0.06f;
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateGoldBurstPrefab(string prefabPath, Material material)
    {
        DeleteAssetIfPresent(prefabPath);
        GameObject root = new GameObject("GoldBurst");
        ParticleSystem system = root.AddComponent<ParticleSystem>();
        ConfigureBurstParticle(system, material, new Color(1f, 0.9f, 0.34f, 0.92f), 18, 0.7f, 0.9f, 1.8f, 0.12f, ParticleSystemShapeType.Sphere);
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateAutumnDriftPrefab(string prefabPath, Material material)
    {
        DeleteAssetIfPresent(prefabPath);
        GameObject root = new GameObject("AutumnDrift");
        ParticleSystem system = root.AddComponent<ParticleSystem>();

        var main = system.main;
        main.loop = true;
        main.duration = 9f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4.8f, 6.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.28f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.8f, 0.46f, 0.18f, 0.52f),
            new Color(0.52f, 0.22f, 0.1f, 0.38f));
        main.maxParticles = 36;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = system.emission;
        emission.rateOverTime = 3.2f;

        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(12f, 4f, 14f);

        var velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.08f, 0.01f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.04f, 0.12f);

        var noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.08f;
        noise.frequency = 0.22f;

        ConfigureParticleRenderer(system, material, 0.18f);
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateGroundFogPrefab(string prefabPath, Material material)
    {
        DeleteAssetIfPresent(prefabPath);
        GameObject root = new GameObject("GroundFog");
        ParticleSystem system = root.AddComponent<ParticleSystem>();

        var main = system.main;
        main.loop = true;
        main.duration = 10f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5.5f, 8.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.14f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.72f, 0.75f, 0.79f, 0.12f));
        main.maxParticles = 90;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = system.emission;
        emission.rateOverTime = 4f;

        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(10f, 0.35f, 14f);

        var colorOverLifetime = system.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
            new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(0.72f, 0.74f, 0.78f), 0f),
                    new GradientColorKey(new Color(0.4f, 0.43f, 0.46f), 1f)
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.16f, 0.2f),
                    new GradientAlphaKey(0f, 1f)
                }
            });

        var noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.14f;
        noise.frequency = 0.14f;

        ConfigureParticleRenderer(system, material, 0.55f);
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void ConfigureBurstParticle(
        ParticleSystem system,
        Material material,
        Color color,
        short burstCount,
        float duration,
        float lifetime,
        float speed,
        float size,
        ParticleSystemShapeType shapeType)
    {
        var main = system.main;
        main.loop = false;
        main.duration = duration;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = color;
        main.maxParticles = burstCount;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = system.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burstCount) });

        var shape = system.shape;
        shape.shapeType = shapeType;
        shape.radius = 0.25f;

        var colorOverLifetime = system.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
            new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(color * 0.7f, 1f)
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(Mathf.Max(0.4f, color.a), 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            });

        ConfigureParticleRenderer(system, material, 0.24f);
    }

    private static void ConfigureParticleRenderer(ParticleSystem system, Material material, float maxParticleSize)
    {
        ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.maxParticleSize = maxParticleSize;
        renderer.minParticleSize = 0.01f;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static GameObject CreateMarker(Transform parent, string name, Vector3 position, Vector3 eulerAngles)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent);
        marker.transform.position = position;
        marker.transform.rotation = Quaternion.Euler(eulerAngles);
        return marker;
    }

    private static GameObject CreatePrimitivePart(
        Transform parent,
        PrimitiveType primitiveType,
        string name,
        Vector3 localPosition,
        Vector3 localEuler,
        Vector3 localScale,
        Material material,
        bool keepCollider)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(localEuler);
        part.transform.localScale = localScale;

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        if (!keepCollider)
        {
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        return part;
    }

    private static void SetStaticRecursive(GameObject root, StaticEditorFlags flags)
    {
        GameObjectUtility.SetStaticEditorFlags(root, flags);
        foreach (Transform child in root.transform)
        {
            SetStaticRecursive(child.gameObject, flags);
        }
    }

    private static void DeleteAssetIfPresent(string assetPath)
    {
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
        {
            AssetDatabase.DeleteAsset(assetPath);
        }
    }

    private static string ToAbsoluteProjectPath(string projectRelativePath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), projectRelativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
