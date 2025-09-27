using UnityEngine;                                      // Unityの基本機能
using UnityEngine.InputSystem;                          // 新Input System
using Cinemachine;                                      // カメラ参照（Transformのみ使用）
using System.Collections;                                // ★追加：コルーチン用

[RequireComponent(typeof(Rigidbody))]                   // Rigidbody必須
[RequireComponent(typeof(Animator))]                    // Animator必須
public class PlayerController : MonoBehaviour
{
    [Header("移動設定")]
    public float moveSpeed = 5f;                        // 地上移動速度
    public float rotationSpeed = 10f;                   // 見た目の回転速度

    [Header("ジャンプ設定")]
    public float jumpForce = 5f;                        // ジャンプ力（Impulse）
    public LayerMask groundMask;                        // 接地判定レイヤー
    public float groundCheckRadius = 0.25f;             // 接地球の半径
    public float groundCheckDistance = 0.3f;            // 下向き距離
    public Transform groundCheckPoint;                  // ★追加：足元の基準（未設定なら自動）
    public float groundCheckYOffset = 0.1f;             // ★追加：足元基準の微調整（上方向）

    [Header("カメラ")]
    public Transform cameraFollowTarget;                // カメラ基準の向き（※プレイヤーの腰付近の空オブジェクト推奨）

    [Header("ゴルフショット")]
    public float shotPower = 15f;                       // ショットのパワー（Impulse）
    public float shotUpwardForce = 0.3f;                // 山なり用の上向き成分
    public LayerMask ballMask;                          // ボール用レイヤー（※現行は検出では未使用。将来のフィルタ等で流用可）

    [Header("クラブ表示")]
    public GameObject loftedClubModel;                  // 山なりクラブの見た目
    public GameObject straightClubModel;                // 直線クラブの見た目

    [Header("ショット用コライダー")]                   // ★追加：インパクト用コライダー（瞬間ON）
    public GameObject shotHitCollider;                  // インパクト時に一瞬出す

    // ===== Aiming改良 =====
    [Header("Aiming（構え中）設定")]                   // ★追加：構え体験のチューニング
    public float orbitTurnSpeed = 120f;                 // ★追加：構え中にターゲットの周りを回る角速度（度/秒）
    public float orbitDistance = 2.0f;                  // ★追加：周回時に維持したい半径（ロック時距離を代入）
    public float orbitHalfSpanDeg = 90f;                // ★追加：周回の片側角（合計180°）
    public float aimEnterMaxDistance = 4f;              // ★追加：構え開始の許容最大距離
    public float aimEnterMinClearance = 0.2f;           // ★追加：ターゲット安全半径に対する追加マージン（押し出し無し）

    [Header("Cinemachine（カメラ切替）")]              // ★追加：FreeLook→右肩VCamへ切替
    public CinemachineFreeLook freeLook;                // ★追加：通常TPS用
    public CinemachineVirtualCamera aimVCam;            // ★追加：構え中に使う「右肩越し」VCam
    public bool useAimVCam = true;                      // ★追加：構え中はこのVCamを使う

    [Header("右肩VCamの見え方")]
    public float aimRightOffset = 0.6f;                 // ★追加：右肩オフセット（+x）
    public float aimUpOffset    = 1.4f;                 // ★追加：高さオフセット（+y）
    public float aimBackDistance = 3.5f;                // ★追加：後方距離（背後）
    public float aimCameraSide   = 1f;                  // ★追加：3rdPersonFollow の CameraSide（1=右肩）

    [Header("カメラ切替の優先度")]
    public int freeLookPriorityPlay = 100;              // ★追加：通常時FreeLookを上位に
    public int aimVCamPriorityAim   = 200;              // ★追加：構え時aimVCamを最上位に
    public int priorityInactive     = 10;               // ★追加：非アクティブ側はこの低優先度へ

    [Header("カメラ安定化（ショット）")]
    public bool holdAimUntilShotEnd = true;             // ★追加：ショット完了までAimVCamを保持
    public float postShotHoldSeconds = 0.2f;            // ★追加：ショット後少しだけ保持してから戻す
    public float postShotReacquireCooldown = 0.25f;     // ★追加：ショット後の再取得禁止時間（ワープ防止）

    [Header("検出連携")]
    public AimDetector aimDetector;                     // ★変更：検出はAimDetectorに集約（ロックを使用）

    // ===== UI（パワーゲージ） =====
    [Header("UI（パワーゲージ）")]                     // ★追加：各プレイヤー専用ゲージ
    public PowerGaugeUI powerGauge;                     // ★追加：このプレイヤー専用のゲージUI（Instanceは使わない）
    public float powerMulMin = 0.5f;                    // ★追加：ゲージ最小時の倍率
    public float powerMulMax = 1.5f;                    // ★追加：ゲージ最大時の倍率

    // ===== スコア（昔の形に復元） =====
    [Header("スコア")]       // 見出し
    public int score = 0; // 現在のスコア
    public System.Action<int,int> OnScored;             // (得点, 合計)

    public void AddScore(int points)                    // （HoleGoalから呼ばれる）
    {
        score += Mathf.Max(0, points);                         // マイナスは防止（必要なら許可に変えてOK）
        Debug.Log($"[Score] {name} +{points}点 → 合計 {score}点"); // デバッグ
        OnScored?.Invoke(points, score);                       //UI更新などに使えるイベント
    }

    // 内部参照
    private Rigidbody rb;                               // 自身のRigidbody
    private Animator animator;                          // 自身のAnimator
    private PlayerInput playerInput;                    // PlayerInput参照

    // 入力アクション
    private InputAction moveAction;                     // Move（Vector2）
    private InputAction jumpAction;                     // Jump（Button）
    private InputAction shotAction;                     // Shot（Button：長押し/離す）
    private InputAction switchShotTypeAction;           // グラブ切替（L1/R1）※あなたの名「Grab」

    // 状態
    private Vector2 inputVector;                        // 移動入力
    private bool isGrounded = false;                    // 接地フラグ
    private bool canJump = true;                        // ジャンプロック
    private bool isAiming = false;                      // 構え中（formアニメ中も含む）
    private bool isShooting = false;                    // スイング中（Shotアニメ中）

    // ★追加：外部ロック（カウントダウン等で一時的に行動禁止）
    private bool externalLock = false;                  // ★追加：外部からのロック状態

    // Aiming用
    private Transform currentAimTarget;                 // ★追加：構え中に周回する“ターゲット”
    private bool   aimHasBaseline = false;              // ★追加：基準ベクトル確定済みか
    private Vector3 aimBaseDirFlat;                     // ★追加：基準ベクトル（ターゲット→プレイヤーの水平）
    private float  aimOffsetDeg = 0f;                   // ★追加：基準からの現在オフセット角（-half〜+half）

    // グラブ種類
    public enum ShotType { Lofted, Straight }           // 山なり / 直線
    private ShotType currentShotType = ShotType.Lofted; // 現在の種類

    // ★追加：ゲージ値の保持（0..1）
    private float lastGauge01 = 0.5f;                   // ★追加：直近のパワーゲージ値
    public float CurrentGauge01 => lastGauge01;         // ★追加：外部参照用
    public float GetPowerMultiplier()                   // ★追加：現在の倍率を返す（他スクリプトからも利用可）
        => Mathf.Lerp(powerMulMin, powerMulMax, lastGauge01);

    void Awake()
    {
        rb = GetComponent<Rigidbody>();                 // Rigidbody取得
        animator = GetComponent<Animator>();            // Animator取得
        playerInput = GetComponent<PlayerInput>();      // PlayerInput取得

        moveAction  = playerInput.actions["Move"];      // Move参照
        jumpAction  = playerInput.actions["Jump"];      // Jump参照
        shotAction  = playerInput.actions["Shot"];      // Shot参照（長押し/離す）
        switchShotTypeAction = playerInput.actions["Grab"]; // グラブ切替参照（L1/R1）

        HideAllClubsAtStart();                          // 開始時はクラブを非表示（移動中は見えない仕様）

        // VCamのFollow/LookAtが未設定なら保険的に設定する
        if (cameraFollowTarget != null)
        {
            if (freeLook != null)
            {
                if (freeLook.Follow == null) freeLook.Follow = cameraFollowTarget;   // ★追加
                if (freeLook.LookAt == null) freeLook.LookAt = cameraFollowTarget;   // ★追加
            }
            if (aimVCam != null)
            {
                if (aimVCam.Follow == null) aimVCam.Follow = cameraFollowTarget;     // ★追加
                if (aimVCam.LookAt == null) aimVCam.LookAt = cameraFollowTarget;     // ★追加
            }
        }

        ApplyCameraPriority(aiming:false);              // 起動時：FreeLook=上位 / aimVCam=下位
        ConfigureAimVCam();                             // 右肩オフセット適用
    }

    void Update()
    {
        inputVector = moveAction.ReadValue<Vector2>();  // 移動入力を読む
        UpdateGroundedState();                          // 接地更新

        float speedPercent = inputVector.magnitude;     // 入力強度0〜1
        animator.SetFloat("speed", speedPercent);       // アニメ用速度
        animator.SetBool("IsGrounded", isGrounded);     // アニメ用接地

        // 構え開始：Shotを押し始めた瞬間（長押し中に維持）
        if (shotAction.IsPressed() && !isAiming && !isShooting && !IsActionLocked()) // ★変更：外部ロック中は不可
        {
            if (aimDetector == null) return;            // ★追加：検出器必須

            // ★追加：自分（プレイヤー）の水平半径を自動計算
            float myRadius = ComputeMyHorizontalRadius();    // ★追加

            // ★変更：AimDetectorに自分の半径も渡してロック判定（足りなければ構え拒否＝押し出し無し）
            if (!aimDetector.TryLockTarget(aimEnterMaxDistance, aimEnterMinClearance, myRadius))
            {
                return;                                 // ★追加：構えに入らない
            }

            // ★追加：ロック対象と固定半径（=ロック時距離）を使用
            currentAimTarget = aimDetector.LockedTarget;               // ★追加：固定ターゲット
            orbitDistance    = Mathf.Max(0.01f, aimDetector.LockedDistanceXZ); // ★追加：固定半径

            isAiming = true;                            // 構えフラグON
            animator.SetBool("form", true);             // formアニメON
            RefreshClubVisual();                        // クラブ表示（構え中ON）

            EnterAimMode();                             // 構え突入（カメラ切替）

            // ★追加：このプレイヤー専用のゲージ表示を開始
            if (powerGauge) powerGauge.Begin(0f);       // 0 からスタート
        }

        // 構え終了→スイング開始：Shotを離した瞬間 
        if (isAiming && shotAction.WasReleasedThisFrame())
        {
            isAiming = false;                           // 構えフラグOFF
            animator.SetBool("form", false);            // formをOFF
            isShooting = true;                          // スイングフラグON
            animator.SetTrigger("shot");                // Shotアニメ開始
            RefreshClubVisual();                        // 見た目維持

            // ★追加：ゲージを確定し、倍率を保持（0..1）
            lastGauge01 = powerGauge ? powerGauge.EndAndGet() : 0.5f;
            // ※ 実際の加速は ShotHitCollider 側で owner.GetPowerMultiplier() を掛ければ連動します
        }

        // 構え中：ターゲット周回（180°制限、押し出し無し／固定半径）
        if (isAiming)
        {
            AimOrbitUpdate();                           // ターゲット周回
        }

        // ジャンプ（構え/スイング中は不可）
        if (jumpAction.triggered && isGrounded && canJump && !IsActionLocked())
        {
            DoJump();                                   // 実ジャンプ
        }

        // グラブ切替（L1/R1：交互トグル）
        // ★変更：構え中（isAiming）は切替OK。スイング中（isShooting）と外部ロック（externalLock）時のみ不可。
        if (switchShotTypeAction.triggered && !isShooting && !externalLock)
        {
            currentShotType =
                (currentShotType == ShotType.Lofted) ? ShotType.Straight : ShotType.Lofted;

            Debug.Log("グラブ切替: " + currentShotType);
            RefreshClubVisual();    // 構え中でも見た目が即時反映される
        }

    }

    void FixedUpdate()
    {
        Move();                                        // 物理移動
    }

    private void Move()
    {
        if (IsActionLocked())                          // 構え or スイング中 or 外部ロックは移動不可
        {
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f); // 水平速度を止める
            return;                                    // 以降の移動処理はしない
        }

        if (inputVector.sqrMagnitude < 0.0001f)        // 入力ほぼゼロ
        {
            Vector3 v = rb.velocity;                   // 現在速度
            rb.velocity = new Vector3(0f, v.y, 0f);    // 水平停止（垂直は重力）
            return;                                    // 終了
        }

        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y); // 入力ベクトル
        float camY = cameraFollowTarget ? cameraFollowTarget.eulerAngles.y : transform.eulerAngles.y; // 基準角
        moveDir = Quaternion.Euler(0f, camY, 0f) * moveDir; // カメラ基準に回す
        moveDir.Normalize();                                 // 長さ1に

        Vector3 targetVel = moveDir * moveSpeed;             // 目標水平速度
        targetVel.y = rb.velocity.y;                         // 垂直は維持
        rb.velocity = targetVel;                             // 適用

        Quaternion toRot = Quaternion.LookRotation(moveDir); // 向き更新
        transform.rotation = Quaternion.Slerp(               // スムーズに補間
            transform.rotation, toRot, rotationSpeed * Time.deltaTime);
    }

    private void UpdateGroundedState()                       // 接地判定
    {
        // ▼足元でのCheckSphere方式（安定）
        Vector3 origin;
        if (groundCheckPoint != null)
            origin = groundCheckPoint.position + Vector3.up * groundCheckYOffset;
        else
            origin = transform.position + Vector3.up * 0.05f; // 少し上から

        bool hit = Physics.CheckSphere(
            origin,
            groundCheckRadius,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
        isGrounded = hit;                                    // 保存
        if (isGrounded) canJump = true;                      // 地上なら解除
    }

    private void DoJump()                                    // 実ジャンプ
    {
        canJump = false;                                     // 多重ジャンプ防止
        Vector3 v = rb.velocity;                             // 現在速度
        v.y = 0f;                                            // 縦をリセット
        rb.velocity = v;                                     // 反映
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse); // 上向きImpulse
        animator.SetTrigger("jump");                         // アニメ側へ
    }

    public void OnJumpEnd()                                  // アニメイベント（ジャンプ終端想定）
    {
        UpdateGroundedState();                               // 念のため更新
        if (isGrounded) canJump = true;                      // 地上なら解除
    }

    // =======================
    // アニメーションイベント群
    // =======================

    public void AnimEvent_FormEnd()                          // formの終端
    {
        // ここでは特別な処理は不要（必要ならフラグ整合を確認）
        // 例：Debug.Log("Form End");
    }

    public void AnimEvent_ShotFire()                         // 発射タイミング
    {
        // ★変更：ショットは「ヒット用コライダー」を一瞬だけONにして処理
        StartCoroutine(ActivateShotColliderMomentarily());   // 一瞬だけコライダーをON
    }

    private IEnumerator ActivateShotColliderMomentarily()    // ★追加：一瞬ONユーティリティ
    {
        if (shotHitCollider != null)
        {
            shotHitCollider.SetActive(true);                 // ON
            yield return new WaitForSeconds(0.1f);           // 少し待つ
            shotHitCollider.SetActive(false);                // OFF
        }
    }

    public void AnimEvent_ShotEnd()                          // スイング完了
    {
        isShooting = false;                                  // スイング終了
        RefreshClubVisual();                                 // 表示更新（非表示へ）

        // ターゲットロックを解除し、一定時間は再取得しない（ワープ/暴発防止）
        if (aimDetector != null)
            aimDetector.ReleaseLock(postShotReacquireCooldown); // ★追加

        // カメラはpostHold後にFreeLookへ（ぶれ防止）
        if (holdAimUntilShotEnd)
            StartCoroutine(ReturnCameraAfterHold(postShotHoldSeconds));
        else
        {
            ApplyCameraPriority(aiming:false);
            SnapFreeLookOnce();
        }

        // 次回構えで基準取り直し
        currentAimTarget = null;                             // ★追加
        aimHasBaseline   = false;                            // ★追加
    }

    private IEnumerator ReturnCameraAfterHold(float holdSec)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, holdSec)); // 待機
        ApplyCameraPriority(aiming:false);                        // FreeLookへ
        SnapFreeLookOnce();                                      // 向き同期
    }

    // =======================
    // 表示系ヘルパー
    // =======================

    private void HideAllClubsAtStart()                       // 起動時は非表示
    {
        if (loftedClubModel)   loftedClubModel.SetActive(false);
        if (straightClubModel) straightClubModel.SetActive(false);
    }

    private void RefreshClubVisual()                         // 現在状態に合わせて表示更新
    {
        bool show = IsActionLocked();                        // 構え or スイング中は表示
        if (loftedClubModel)
            loftedClubModel.SetActive(show && currentShotType == ShotType.Lofted);
        if (straightClubModel)
            straightClubModel.SetActive(show && currentShotType == ShotType.Straight);
    }

    private bool IsActionLocked()                            // 行動ロック条件
    {
        return externalLock || isAiming || isShooting;       // ★変更：外部ロックも考慮
    }

    private void OnDrawGizmosSelected()                      // デバッグ可視化
    {
        Gizmos.color = Color.red;                            // 色
        Vector3 origin = transform.position + Vector3.up * 0.05f; // 始点
        Gizmos.DrawWireSphere(origin + Vector3.down * groundCheckDistance, groundCheckRadius); // 半径表示
    }

    // =======================
    // Aiming処理（180°制限、押し出し無し・固定半径）
    // =======================

    private void EnterAimMode()                              // 構え突入時
    {
        ApplyCameraPriority(aiming:true);                    // aimVCamを上位に
        SnapAimVCamOnce();                                   // 切替瞬間に一度だけ背後右へスナップ

        aimHasBaseline = false;                              // 基準リセット
        aimOffsetDeg   = 0f;                                 // 角度オフセット初期化
    }

    private void AimOrbitUpdate()                            // 構え中ターゲット周回
    {
        // ターゲットがいない場合：カメラ前へ向けておく（見た目安定）
        if (currentAimTarget == null)
        {
            if (cameraFollowTarget)
            {
                Vector3 flatForward = cameraFollowTarget.forward; flatForward.y = 0f; // 水平成分
                if (flatForward.sqrMagnitude > 0.0001f)
                {
                    Quaternion toRot = Quaternion.LookRotation(flatForward);
                    transform.rotation = Quaternion.Slerp(transform.rotation, toRot, rotationSpeed * Time.deltaTime);
                }
            }
            return;
        }

        // 基準ベクトル（ターゲット→自分の水平）を初回確定
        if (!aimHasBaseline)
        {
            Vector3 baseDir = transform.position - currentAimTarget.position; baseDir.y = 0f;
            if (baseDir.sqrMagnitude < 0.0001f) baseDir = transform.forward;
            aimBaseDirFlat = baseDir.normalized;
            aimHasBaseline = true;
            aimOffsetDeg   = 0f;
        }

        // 入力で±回転（度/秒）→180°（±90°）に制限
        float turnX = inputVector.x;
        if (Mathf.Abs(turnX) > 0.01f)
        {
            aimOffsetDeg += turnX * orbitTurnSpeed * Time.deltaTime;
            aimOffsetDeg  = Mathf.Clamp(aimOffsetDeg, -orbitHalfSpanDeg, +orbitHalfSpanDeg);
        }

        // 固定半径で回る（押し出し/引き寄せは一切しない）
        Vector3 center = currentAimTarget.position;
        Vector3 dir    = Quaternion.AngleAxis(aimOffsetDeg, Vector3.up) * aimBaseDirFlat;
        Vector3 newPos = center + dir.normalized * orbitDistance;  // ★固定半径（ロック時距離）
        newPos.y = transform.position.y;                            // 高さは現状維持
        transform.position = newPos;                                // 位置を更新

        // 常にターゲット方向を向く
        Vector3 lookDir = center - transform.position; lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion toRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, toRot, rotationSpeed * Time.deltaTime);
        }
    }

    // ===== カメラ切替ユーティリティ =====

    private void ApplyCameraPriority(bool aiming)           // 優先度で確実に切替
    {
        if (useAimVCam && aimVCam != null)
        {
            aimVCam.Priority = aiming ? aimVCamPriorityAim : priorityInactive;
            aimVCam.PreviousStateIsValid = false;           // 履歴破棄＝即スナップ
        }
        if (freeLook != null)
        {
            freeLook.Priority = aiming ? priorityInactive : freeLookPriorityPlay;
            freeLook.PreviousStateIsValid = false;          // 履歴破棄
        }
    }

    private void SnapAimVCamOnce()                          // 右肩VCamを一度だけ“背後右”に
    {
        if (aimVCam == null) return;
        ConfigureAimVCam();                                 // 右肩オフセット反映
        // 以後は毎フレームいじらない（スピン/ブレ防止）
    }

    private void SnapFreeLookOnce()                         // FreeLookへ戻す時に一度だけYaw合わせ
    {
        if (freeLook == null) return;
        float yawDeg = Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg; // プレイヤーYaw
        freeLook.m_XAxis.Value = yawDeg;                    // X軸だけ同期（ぐるぐる防止）
    }

    private void ConfigureAimVCam()                          // 右肩VCamの各種オフセットを適用
    {
        if (aimVCam == null) return;

        var tpf = aimVCam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        if (tpf != null)
        {
            tpf.CameraSide   = Mathf.Clamp01(aimCameraSide);               // 1=右肩
            tpf.ShoulderOffset = new Vector3(aimRightOffset, aimUpOffset, 0f);
            tpf.CameraDistance     = Mathf.Max(0.1f, aimBackDistance);
            tpf.Damping      = new Vector3(0.05f, 0.05f, 0.1f);
            return;
        }

        var transposer = aimVCam.GetCinemachineComponent<CinemachineTransposer>();
        if (transposer != null)
        {
            transposer.m_FollowOffset = new Vector3(aimRightOffset, aimUpOffset, -Mathf.Abs(aimBackDistance));
            transposer.m_XDamping = transposer.m_YDamping = transposer.m_ZDamping = 0.05f;
        }
    }

    // ===== ユーティリティ：自分の水平半径を自動計算 =====
    private float ComputeMyHorizontalRadius()               // ★追加：自身の「水平半径」を推定
    {
        float maxR = 0.25f;                                 // 最低目安
        var cols = GetComponentsInChildren<Collider>();
        for (int i = 0; i < cols.Length; i++)
        {
            var c = cols[i];
            if (c == null) continue;
            if (c.enabled == false) continue;
            // ※ プレイヤーの自身のトリガーは基本含めてOK（めり込み判定には影響小）
            Vector3 e = c.bounds.extents;                   // ワールド半サイズ
            float r = Mathf.Sqrt(e.x * e.x + e.z * e.z);    // 水平半径 ≒ √(x^2+z^2)
            if (r > maxR) maxR = r;
        }
        return maxR;
    }

    // ===== 公開アクセサ =====

    public bool IsLofted()                                  // 現在Loftedかどうか返す
    {
        return currentShotType == ShotType.Lofted;          // 列挙型をそのまま利用
    }

    public bool IsAiming()                                  // ★追加：AimDetector から参照される
    {
        return isAiming;                                    // 構え中かどうか
    }

    public void SetCurrentAimTarget(Transform t)            // ★追加：AimDetector（Proxy）連携用
    {
        currentAimTarget = t;                               // 周回中心を受け取る
    }

    public void SetCurrentAimOrbitRadius(float r)           // ★追加：AimDetector（Proxy）連携用
    {
        orbitDistance = Mathf.Max(0.01f, r);                // 安全半径を受け取る
    }

    public void SetExternalActionLock(bool locked)
    {
        externalLock = locked;                              // ロック状態を更新

        // ★追加：rbが未キャッシュならここで遅延取得（Awake前でも落ちない）
        if (!rb) rb = GetComponent<Rigidbody>();

        if (locked && rb)                                   // 水平速度を止める（rbがあるときだけ）
        {
            Vector3 v = rb.velocity;
            rb.velocity = new Vector3(0f, v.y, 0f);
        }
        // 必要であれば Animator や UI のロック表示をここで切り替え可
    }

}
