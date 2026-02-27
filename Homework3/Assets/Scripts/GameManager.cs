using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    const int StageCount = 3;

    int score;
    int remainingTargets;
    int totalTargets;
    int stageRemainingTargets;
    int currentStage;
    int shotsFired;
    int shotsHit;
    bool courseActive;

    [Header("UI Placement")]
    [SerializeField] Vector3 scoreSignStageOffset = new Vector3(2.8f, 0f, 1.2f);
    [SerializeField] Vector3 scoreboardLocalOffsetOnSign = new Vector3(0f, 2.1f, -0.07f);
    [SerializeField] float scoreSignYaw = 90f;

    Text scoreText;
    Text accuracyText;
    Text messageText;
    Transform scoreboardTransform;
    Transform scoreSignTransform;
    Transform xrOrigin;
    RobotHand rightRobotHand;
    bool wasRightSecondaryPressed;
    List<GameObject> targets = new List<GameObject>();
    List<GameObject> spawnedWeapons = new List<GameObject>();
    readonly List<GameObject>[] stageTargets =
    {
        new List<GameObject>(),
        new List<GameObject>(),
        new List<GameObject>()
    };

    struct TargetHome
    {
        public GameObject target;
        public Vector3 position;
        public Quaternion rotation;
        public int stage;
    }
    List<TargetHome> targetHomes = new List<TargetHome>();

    struct WeaponHome
    {
        public GameObject weapon;
        public Vector3 position;
        public Quaternion rotation;
    }
    List<WeaponHome> weaponHomes = new List<WeaponHome>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        SetupRobotHands();
        FindSceneReferences();
        StartCourse();
    }

    void Update()
    {
        if (rightRobotHand == null)
            return;

        bool isPressed = rightRobotHand.SecondaryButtonPressed;
        if (isPressed && !wasRightSecondaryPressed)
            OnRestartPressed();

        wasRightSecondaryPressed = isPressed;
    }

    // ---- XR Hand Setup ----

    void SetupRobotHands()
    {
        string[] originNames = { "XR Origin (XR Rig)", "XR Origin (VR)", "XR Origin" };
        var origins = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (var t in origins)
        {
            foreach (string originName in originNames)
            {
                if (t.name == originName)
                {
                    xrOrigin = t;
                    break;
                }
            }
            if (xrOrigin != null) break;
        }

        if (xrOrigin == null)
        {
            Debug.LogError("GameManager: XR Origin not found!");
            return;
        }

        Transform cameraOffset = xrOrigin.Find("Camera Offset");
        if (cameraOffset == null)
        {
            Debug.LogError("GameManager: Camera Offset not found!");
            return;
        }

        Transform leftController = cameraOffset.Find("Left Controller") ?? cameraOffset.Find("Left Hand");
        Transform rightController = cameraOffset.Find("Right Controller") ?? cameraOffset.Find("Right Hand");

        if (leftController != null)
        {
            if (leftController.gameObject.GetComponent<RobotHand>() == null)
                leftController.gameObject.AddComponent<RobotHand>();
            if (leftController.gameObject.GetComponent<GrabThrow>() == null)
                leftController.gameObject.AddComponent<GrabThrow>();
        }

        if (rightController != null)
        {
            var rightHand = rightController.gameObject.GetComponent<RobotHand>();
            if (rightHand == null)
                rightHand = rightController.gameObject.AddComponent<RobotHand>();
            rightRobotHand = rightHand;

            if (rightController.gameObject.GetComponent<GrabThrow>() == null)
                rightController.gameObject.AddComponent<GrabThrow>();
        }
    }

    // ---- Scene References ----

    void FindSceneReferences()
    {
        // Scoreboard
        var scoreboard = GameObject.Find("Scoreboard");
        if (scoreboard != null)
        {
            scoreboardTransform = scoreboard.transform;
            var st = scoreboard.transform.Find("ScoreText");
            if (st != null) scoreText = st.GetComponent<Text>();
            var at = scoreboard.transform.Find("AccuracyText");
            if (at != null) accuracyText = at.GetComponent<Text>();
            var mt = scoreboard.transform.Find("MessageText");
            if (mt != null) messageText = mt.GetComponent<Text>();
        }

        var scoreSign = GameObject.Find("ScoreSign");
        if (scoreSign != null)
            scoreSignTransform = scoreSign.transform;

        // Targets — store home positions for reset
        targets.Clear();
        targetHomes.Clear();
        for (int i = 0; i < StageCount; i++)
            stageTargets[i].Clear();

        foreach (var t in FindObjectsByType<Target>(FindObjectsSortMode.None))
        {
            int stage = GetStageForTarget(t.gameObject.name, t.transform.position.z);
            targets.Add(t.gameObject);
            targetHomes.Add(new TargetHome
            {
                target = t.gameObject,
                position = t.transform.position,
                rotation = t.transform.rotation,
                stage = stage
            });
            stageTargets[stage - 1].Add(t.gameObject);
        }
        totalTargets = targets.Count;

        // Weapons — store home positions for reset
        spawnedWeapons.Clear();
        weaponHomes.Clear();
        foreach (var p in FindObjectsByType<Pistol>(FindObjectsSortMode.None))
        {
            var weapon = p.gameObject;
            spawnedWeapons.Add(weapon);
            weaponHomes.Add(new WeaponHome
            {
                weapon = weapon,
                position = p.transform.position,
                rotation = p.transform.rotation
            });

            var rb = weapon.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // Reset buttons
        foreach (var pb in FindObjectsByType<PhysicsButton>(FindObjectsSortMode.None))
        {
            if (pb.label == "RESET")
            {
                pb.onPressed.AddListener(OnRestartPressed);
            }
        }

        UpdateScoreUI();
    }

    // ---- Scoring ----

    public void RegisterShot()
    {
        if (!courseActive) return;
        shotsFired++;
        UpdateScoreUI();
    }

    public void OnTargetDestroyed(int points)
    {
        if (!courseActive) return;

        score += points;
        shotsHit++;
        remainingTargets--;
        stageRemainingTargets--;
        UpdateScoreUI();

        if (stageRemainingTargets <= 0)
        {
            if (currentStage < StageCount)
            {
                AdvanceToNextStage();
            }
            else
            {
                courseActive = false;
                if (messageText != null)
                    messageText.text = "VICTORY! Final Score: " + GetFinalScore() + " (Accuracy: " + GetAccuracy() + "%)";
            }
        }
    }

    string GetAccuracy()
    {
        if (shotsFired <= 0) return "0";
        return Mathf.RoundToInt((float)shotsHit / shotsFired * 100f).ToString();
    }

    int GetFinalScore()
    {
        float accuracyFactor = shotsFired <= 0 ? 0f : (float)shotsHit / shotsFired;
        return Mathf.RoundToInt(score * accuracyFactor);
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Stage: " + currentStage + "/" + StageCount + "  |  Score: " + score + "  |  Targets Left: " + remainingTargets + "/" + totalTargets;
        if (accuracyText != null)
        {
            string acc = GetAccuracy();
            accuracyText.text = "Accuracy: " + acc + "% (" + shotsHit + "/" + shotsFired + ")";
        }
    }

    void OnRestartPressed()
    {
        // Reactivate and reset all targets to their original positions
        for (int i = 0; i < targetHomes.Count; i++)
        {
            GameObject t = targetHomes[i].target;
            if (t == null) continue;
            t.SetActive(true);
            t.transform.position = targetHomes[i].position;
            t.transform.rotation = targetHomes[i].rotation;
            var rb = t.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }

        // Reset weapons back to rack positions
        for (int i = 0; i < weaponHomes.Count; i++)
        {
            GameObject w = weaponHomes[i].weapon;
            if (w == null) continue;
            w.transform.SetParent(null);
            w.transform.position = weaponHomes[i].position;
            w.transform.rotation = weaponHomes[i].rotation;
            var rb = w.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            var throwable = w.GetComponent<ThrowableObject>();
            if (throwable != null)
                throwable.isHeld = false;
        }

        StartCourse();
    }

    void StartCourse()
    {
        score = 0;
        shotsFired = 0;
        shotsHit = 0;
        remainingTargets = totalTargets;
        currentStage = 1;
        courseActive = true;

        ActivateCurrentStageTargets();
        TeleportPlayerToStage(currentStage);
        PositionStageUI(currentStage);

        if (messageText != null)
            messageText.text = "Stage 1: clear all targets to unlock Stage 2.";

        UpdateScoreUI();
    }

    void AdvanceToNextStage()
    {
        currentStage++;
        ActivateCurrentStageTargets();
        TeleportPlayerToStage(currentStage);
        PositionStageUI(currentStage);

        if (messageText != null)
            messageText.text = "Stage " + currentStage + ": clear all targets to continue.";

        UpdateScoreUI();
    }

    void ActivateCurrentStageTargets()
    {
        stageRemainingTargets = 0;

        for (int i = 0; i < targetHomes.Count; i++)
        {
            GameObject t = targetHomes[i].target;
            if (t == null) continue;

            bool shouldBeActive = targetHomes[i].stage == currentStage;
            t.SetActive(shouldBeActive);
            if (shouldBeActive)
                stageRemainingTargets++;
        }
    }

    void TeleportPlayerToStage(int stage)
    {
        if (xrOrigin == null) return;

        Vector3 targetPosition = GetStageStartPosition(stage);
        targetPosition.y = xrOrigin.position.y;

        var characterController = xrOrigin.GetComponent<CharacterController>();
        if (characterController != null)
            characterController.enabled = false;

        xrOrigin.position = targetPosition;

        if (characterController != null)
            characterController.enabled = true;
    }

    Vector3 GetStageStartPosition(int stage)
    {
        GameObject marker = GameObject.Find("StageStart_" + stage);
        if (marker != null)
            return marker.transform.position;

        if (stage == 1) return new Vector3(0f, 0f, 1.6f);
        if (stage == 2) return new Vector3(0f, 0f, 8.0f);
        return new Vector3(0f, 0f, 15.2f);
    }

    int GetStageForTarget(string targetName, float zPos)
    {
        if (targetName.Contains("_10_")) return 1;
        if (targetName.Contains("_20_")) return 2;
        if (targetName.Contains("_30_")) return 3;

        if (zPos < 6.8f) return 1;
        if (zPos < 9.8f) return 2;
        return 3;
    }

    void PositionStageUI(int stage)
    {
        if (scoreboardTransform == null && scoreSignTransform == null)
            return;

        Vector3 anchor = GetStageStartPosition(stage);
        Vector3 rightSignPosition = anchor + scoreSignStageOffset;
        Quaternion rightSignRotation = Quaternion.Euler(0f, scoreSignYaw, 0f);

        if (scoreSignTransform != null)
        {
            scoreSignTransform.position = rightSignPosition;
            scoreSignTransform.rotation = rightSignRotation;
        }

        if (scoreboardTransform != null)
        {
            if (scoreSignTransform != null)
            {
                scoreboardTransform.position = scoreSignTransform.TransformPoint(scoreboardLocalOffsetOnSign);
                scoreboardTransform.rotation = rightSignRotation;
            }
            else
            {
                scoreboardTransform.position = rightSignPosition + (rightSignRotation * scoreboardLocalOffsetOnSign);
                scoreboardTransform.rotation = rightSignRotation;
            }
        }
    }
}
