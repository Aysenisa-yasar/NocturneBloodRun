using System;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nocturne
{
    public class NocturneScenarioDirector : MonoBehaviour
    {
        [SerializeField] private GameObject[] survivorPrefabs = Array.Empty<GameObject>();
        [SerializeField] private Transform[] survivorSpawnPoints = Array.Empty<Transform>();
        [SerializeField] private GameObject monsterPrefab;
        [SerializeField] private Transform monsterSpawnPoint;
        [SerializeField] private GameObject goldPrefab;
        [SerializeField] private Transform[] goldSpawnPoints = Array.Empty<Transform>();
        [SerializeField] private CameraRigFollow cameraRig;
        [SerializeField] private GameHud hud;
        [SerializeField] private ParticleSystem spawnEffectPrefab;
        [SerializeField] private ParticleSystem bloodBurstPrefab;
        [SerializeField] private ParticleSystem muzzleFlashPrefab;
        [SerializeField] private ParticleSystem impactBurstPrefab;
        [SerializeField] private ParticleSystem goldCollectBurstPrefab;
        [SerializeField] private GameObject shotTracerPrefab;
        [SerializeField] private int goldScoreValue = 10;
        [SerializeField] private int weaponUnlockScore = 50;

        private SurvivorAgent[] spawnedSurvivors = Array.Empty<SurvivorAgent>();
        private MonsterAgent spawnedMonster;
        private int teamScore;
        private int totalGold;
        private bool weaponsUnlocked;
        private bool gameStarted;
        private bool gameplayEnded;

        private void Start()
        {
            if (!IsConfigurationReady())
            {
                Debug.LogWarning("Nocturne scenario setup is incomplete.");
                return;
            }

            SpawnScenario();
            PrepareStartState();
            RefreshHud();
        }

        public void BeginGame()
        {
            if (gameStarted || gameplayEnded)
            {
                return;
            }

            gameStarted = true;
            SetGameplayActive(true);
            hud?.ShowInfoPanel(true);
            hud?.ShowStartPanel(false);
            hud?.ShowRestartButton(true);
            hud?.SetStatusText(string.Empty, Color.white);
            RefreshHud();
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.path);
        }

        public void RegisterGoldPickup(SurvivorAgent survivor, int scoreAmount)
        {
            if (!gameStarted || gameplayEnded)
            {
                return;
            }

            totalGold++;
            teamScore += scoreAmount;

            if (!weaponsUnlocked && teamScore >= weaponUnlockScore)
            {
                weaponsUnlocked = true;
                foreach (SurvivorAgent player in spawnedSurvivors)
                {
                    if (player != null)
                    {
                        player.SetWeaponsUnlocked(true);
                    }
                }

                hud?.SetStatusText("Silah kilidi acildi! Simdi canavari vurabilirsiniz.", new Color(1f, 0.84f, 0.29f));
            }

            RefreshHud();
        }

        public void NotifySurvivorEaten(SurvivorAgent survivor)
        {
            if (gameplayEnded)
            {
                return;
            }

            EndGame($"Canavar {survivor.SurvivorName} karakterini yedi. Oyun bitti.", false);
        }

        public void NotifySurvivorEscaped(SurvivorAgent survivor)
        {
            if (gameplayEnded)
            {
                return;
            }

            hud?.SetStatusText($"{survivor.SurvivorName} cikis kapisina ulasti.", new Color(0.75f, 0.93f, 1f));
        }

        public void NotifyMonsterDefeated()
        {
            if (gameplayEnded)
            {
                return;
            }

            EndGame("Canavar dusuruldu. Kasaba simdilik guvende!", true);
        }

        public void RefreshHud()
        {
            if (hud == null)
            {
                return;
            }

            StringBuilder builder = new StringBuilder();
            if (!gameStarted)
            {
                builder.AppendLine("Nocturne Blood Run");
                builder.AppendLine("Kontroller:");
                foreach (SurvivorAgent survivor in spawnedSurvivors)
                {
                    if (survivor != null)
                    {
                        builder.AppendLine(survivor.GetControlSummary());
                    }
                }

                builder.AppendLine();
                builder.AppendLine("Baslat butonuna basarak oyunu baslat.");
            }
            else
            {
                builder.AppendLine($"Toplam Altin: {totalGold}");
                builder.AppendLine($"Skor: {teamScore}");
                builder.AppendLine(weaponsUnlocked
                    ? "Silah: Acik"
                    : $"Silah Kilidi: {teamScore}/{weaponUnlockScore}");

                if (spawnedMonster != null)
                {
                    builder.AppendLine($"Canavar Cani: {spawnedMonster.CurrentHealth}/{spawnedMonster.MaxHealth}");
                }

                foreach (SurvivorAgent survivor in spawnedSurvivors)
                {
                    if (survivor != null)
                    {
                        builder.AppendLine($"{survivor.SurvivorName} Altin: {survivor.CollectedGold}");
                    }
                }
            }
            hud.SetMainText(builder.ToString());
        }

        private bool IsConfigurationReady()
        {
            return survivorPrefabs.Length >= 2
                && survivorSpawnPoints.Length >= 2
                && monsterPrefab != null
                && monsterSpawnPoint != null
                && goldPrefab != null
                && goldSpawnPoints.Length > 0;
        }

        private void SpawnScenario()
        {
            teamScore = 0;
            totalGold = 0;
            weaponsUnlocked = false;
            gameStarted = false;
            gameplayEnded = false;

            int survivorCount = Mathf.Min(survivorPrefabs.Length, survivorSpawnPoints.Length);
            spawnedSurvivors = new SurvivorAgent[survivorCount];

            for (int index = 0; index < survivorCount; index++)
            {
                Transform spawnPoint = survivorSpawnPoints[index];
                GameObject survivorObject = Instantiate(survivorPrefabs[index], spawnPoint.position, spawnPoint.rotation);

                if (spawnEffectPrefab != null)
                {
                    Instantiate(spawnEffectPrefab, spawnPoint.position, Quaternion.identity);
                }

                spawnedSurvivors[index] = survivorObject.GetComponent<SurvivorAgent>();
            }

            GameObject monsterObject = Instantiate(monsterPrefab, monsterSpawnPoint.position, monsterSpawnPoint.rotation);
            if (spawnEffectPrefab != null)
            {
                Instantiate(spawnEffectPrefab, monsterSpawnPoint.position, Quaternion.identity);
            }

            spawnedMonster = monsterObject.GetComponent<MonsterAgent>();
            spawnedMonster.Initialize(spawnedSurvivors, this, bloodBurstPrefab, impactBurstPrefab);

            for (int index = 0; index < spawnedSurvivors.Length; index++)
            {
                SurvivorAgent.ControlScheme scheme = index == 0
                    ? SurvivorAgent.ControlScheme.Wasd
                    : SurvivorAgent.ControlScheme.ArrowKeys;

                spawnedSurvivors[index].Configure(
                    scheme,
                    this,
                    spawnedMonster,
                    bloodBurstPrefab,
                    muzzleFlashPrefab,
                    impactBurstPrefab,
                    shotTracerPrefab);

                spawnedSurvivors[index].SetControllable(false);
            }

            foreach (Transform spawnPoint in goldSpawnPoints)
            {
                GameObject goldObject = Instantiate(goldPrefab, spawnPoint.position, spawnPoint.rotation);
                GoldCollectible collectible = goldObject.GetComponent<GoldCollectible>();
                if (collectible != null)
                {
                    collectible.Configure(goldScoreValue, goldCollectBurstPrefab);
                }
            }

            if (cameraRig != null)
            {
                Transform[] targets = new Transform[spawnedSurvivors.Length + 1];
                for (int index = 0; index < spawnedSurvivors.Length; index++)
                {
                    targets[index] = spawnedSurvivors[index].transform;
                }

                targets[targets.Length - 1] = spawnedMonster.transform;
                cameraRig.SetTargets(targets);
            }

            SetGameplayActive(false);
            hud?.BindButtons(BeginGame, RestartGame);
            hud?.SetStartButtonLabel("Baslat");
            hud?.SetRestartButtonLabel("Yeniden Baslat");
            hud?.ShowRestartButton(false);
            hud?.SetStatusText(string.Empty, Color.white);
        }

        private void EndGame(string message, bool victory)
        {
            gameplayEnded = true;
            SetGameplayActive(false);

            RefreshHud();
            hud?.SetStatusText(
                message,
                victory ? new Color(0.48f, 1f, 0.57f) : new Color(1f, 0.42f, 0.38f));
            hud?.ShowRestartButton(true);
            hud?.ShowStartPanel(false);
        }

        private void PrepareStartState()
        {
            hud?.ShowInfoPanel(false);
            hud?.ShowStartPanel(true);
            hud?.SetStartMessage("Nocturne Blood Run\nKasabadan ve ormandan kac, altin topla, sonra canavari indir.");
            hud?.ShowRestartButton(false);
        }

        private void SetGameplayActive(bool enabled)
        {
            foreach (SurvivorAgent survivor in spawnedSurvivors)
            {
                if (survivor != null)
                {
                    survivor.SetControllable(enabled);
                }
            }

            if (spawnedMonster != null)
            {
                spawnedMonster.SetBehaviourEnabled(enabled);
            }
        }
    }
}
