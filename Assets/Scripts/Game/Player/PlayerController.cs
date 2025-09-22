using UnityEngine;                                      // Unityの基本機能
using UnityEngine.InputSystem;                          // 新Input System
using Cinemachine;                                      // カメラ参照（Transformのみ使用）

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

    [Header("ショット用コライダー")]                   // インパクト用コライダー（瞬間ON）
    public GameObject shotHitCollider;                  // インパクト時に一瞬出す

    // ===== ここからAiming改良 用の追加項目 =====
    [Header("Aiming（構え中）設定")]                   // 構え体験のチューニング
    public float orbitTurnSpeed = 120f;                 // 構え中にターゲットの周りを回る角速度（度/秒）
    public float orbitDistance = 2.0f;                  // 周回時に維持したい半径（AimDetectorから更新される）
    public float orbitHalfSpanDeg = 90f;                // 周回の片側角（合計180°）
    public float aimEnterMaxDistance = 4f;              // 構えに入るための最大距離（これより遠い対象では構え不可）
    public bool  orbitOnlyPushOut = true;               // 近すぎる時だけ外へ押し出す／遠い時は引き寄せない（ワープ防止）

    [Header("Cinemachine（カメラ切替）")]              // FreeLook→右肩VCamへ切替
    public CinemachineFreeLook freeLook;                // 通常TPS用
    public CinemachineVirtualCamera aimVCam;            // 構え中に使う「右肩越し」VCam
    public bool useAimVCam = true;                      // 構え中はこのVCamを使う

    [Header("右肩VCamの見え方")]                        // 右肩量など
    public float aimRightOffset = 0.6f;                 // 右肩オフセット（+x）
    public float aimUpOffset    = 1.4f;                 // 高さオフセット（+y）
    public float aimBackDistance = 3.5f;                // 後方距離（背後）
    public float aimCameraSide   = 1f;                  // 3rdPersonFollow の CameraSide（1=右肩）

    [Header("カメラ切替の優先度")]                      // 確実な切替のため固定値で管理
    public int freeLookPriorityPlay = 100;              // 通常時FreeLookを上位に
    public int aimVCamPriorityAim   = 200;              // 構え時aimVCamを最上位に
    public int priorityInactive     = 10;               // 非アクティブ側はこの低優先度へ

    [Header("カメラ安定化（ショット）")]                // ぶれ対策
    public bool holdAimUntilShotEnd = true;             // ショット完了までAimVCamを保持
    public float postShotHoldSeconds = 0.2f;            // ショット後少しだけ保持してから戻す

    [Header("検出連携")]                                 // ターゲット未検出なら構え禁止
    public AimDetector aimDetector;                     // 常時検出トリガー

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

    // ★追加：Aiming用の状態
    private Transform currentAimTarget;                 // 構え中に周回する“ターゲット”

    // ★追加：オービット制御（180°制限のため）
    private bool   aimHasBaseline = false;              // 基準ベクトルを確定済みか
    private Vector3 aimBaseDirFlat;                     // 基準ベクトル（ターゲット→プレイヤーの水平）
    private float  aimOffsetDeg = 0f;                   // 基準からの現在オフセット角（-half〜+half）

    // グラブ種類
    public enum ShotType { Lofted, Straight }           // 山なり / 直線
    private ShotType currentShotType = ShotType.Lofted; // 現在の種類

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

        // ★追加：VCamのFollow/LookAtが未設定なら保険的に設定する
        if (cameraFollowTarget != null)
        {
            if (freeLook != null)
            {
                if (freeLook.Follow == null) freeLook.Follow = cameraFollowTarget;
                if (freeLook.LookAt == null) freeLook.LookAt = cameraFollowTarget;
            }
            if (aimVCam != null)
            {
                if (aimVCam.Follow == null) aimVCam.Follow = cameraFollowTarget;
                if (aimVCam.LookAt == null) aimVCam.LookAt = cameraFollowTarget;
            }
        }

        // ★追加：起動時の優先度を明示（FreeLook優先）
        ApplyCameraPriority(aiming:false);              // FreeLook=上位 / aimVCam=下位
        ConfigureAimVCam();                             // 右肩オフセット適用
    }

    void Update()
    {
        inputVector = moveAction.ReadValue<Vector2>();  // 移動入力を読む
        UpdateGroundedState();                          // 接地更新

        float speedPercent = inputVector.magnitude;     // 入力強度0〜1
        animator.SetFloat("speed", speedPercent);       // アニメ用速度
        animator.SetBool("IsGrounded", isGrounded);     // アニメ用接地

        //構え開始：Shotを押し始めた瞬間（長押し中に維持）
        if (shotAction.IsPressed() && !isAiming && !isShooting) // まだ構えておらず、スイング中でもない
        {
            // 近くにターゲットが無い場合は構え自体を禁止（ぐるぐる防止＆ワープ防止）
            if (!CanEnterAim())                                   // ★追加
            {
                return;                                           // 構えに入らない
            }

            isAiming = true;                            // 構えフラグON
            animator.SetBool("form", true);             // formアニメON（あなたのパラメ名に合わせて"form"）
            // ★追加：ゲージ開始（このプレイヤーを渡す）
            if (PowerGaugeUI.Instance) PowerGaugeUI.Instance.Begin(this);
            RefreshClubVisual();                        // クラブ表示（構え中ON）

            EnterAimMode();                             
        }

        //構え終了→スイング開始：Shotを離した瞬間 
        if (isAiming && shotAction.WasReleasedThisFrame()) // 構え中に指を離した
        {
            isAiming = false;                           // 構えフラグOFF（formアニメは終了方向へ）
            animator.SetBool("form", false);            // formをOFF（遷移）
            isShooting = true;                          // スイングフラグON（移動ロック継続）
            animator.SetTrigger("shot");                // Shotアニメを開始（Animatorに"shot"トリガーを用意）
            // ★追加：ゲージ確定→倍率をShotPowerBufferへ保存
            if (PowerGaugeUI.Instance) PowerGaugeUI.Instance.Commit(this);
            RefreshClubVisual();                        // 表示継続（isShooting=trueのため表示維持）
        }

        //ジャンプ（構え/スイング中は不可）
        if (jumpAction.triggered && isGrounded && canJump && !IsActionLocked()) // ロック中でない
        {
            DoJump();                                   // 実ジャンプ
        }

        //グラブ切替（L1/R1：交互トグル）
        if (switchShotTypeAction.triggered)            // 押された瞬間
        {
            currentShotType =                          // 山なり↔直線
                (currentShotType == ShotType.Lofted) ? ShotType.Straight : ShotType.Lofted;

            Debug.Log("グラブ切替: " + currentShotType);
            RefreshClubVisual();                       // 構え/スイング中なら見た目即時反映
        }

        // 構え中の「周回制御」（最寄りターゲットの周りを回りつつ距離維持・180°制限）
        if (isAiming)
        {
            AimOrbitUpdate();                          // ターゲット周回の見せ方（★ワープ防止対応済み）
        }
    }

    void FixedUpdate()
    {
        Move();                                        // 物理移動
    }

    private void Move()
    {
        if (IsActionLocked())                          // 構え中 or スイング中は移動不可
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
        StartCoroutine(ActivateShotColliderMomentarily());   // ★追加：一瞬だけコライダーをON
    }

    private System.Collections.IEnumerator ActivateShotColliderMomentarily()
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

        // ★追加：ここでpostHold後にFreeLookへ戻す（ぶれ防止の決定打）
        if (holdAimUntilShotEnd)
            StartCoroutine(ReturnCameraAfterHold(postShotHoldSeconds));
        else
        {
            ApplyCameraPriority(aiming:false);
            SnapFreeLookOnce();
            currentAimTarget = null;                         // 前回ターゲット拘束を解除
            aimHasBaseline   = false;                        // 次回構えで基準取り直し
        }

        // これで IsActionLocked() が false になり、移動・ジャンプが解禁される
        // 例：Debug.Log("Shot End");
    }

    private System.Collections.IEnumerator ReturnCameraAfterHold(float holdSec) // ★追加：保持後に戻す
    {
        yield return new WaitForSeconds(Mathf.Max(0f, holdSec)); // 少し待ってから
        ApplyCameraPriority(aiming:false);                        // FreeLookへ
        SnapFreeLookOnce();                                      // 向き合わせ
        currentAimTarget = null;                                 // 前回ターゲット解除（ワープ防止）
        aimHasBaseline   = false;                                // 基準も解除
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
        return isAiming || isShooting;                       // どちらか中はロック
    }

    private void OnDrawGizmosSelected()                      // デバッグ可視化
    {
        Gizmos.color = Color.red;                            // 色
        Vector3 origin = transform.position + Vector3.up * 0.05f; // 始点
        Gizmos.DrawWireSphere(origin + Vector3.down * groundCheckDistance, groundCheckRadius); // 半径表示
    }

    // =======================
    // ★追加：Aiming用の処理（180°制限付き）
    // =======================

    private void EnterAimMode()                              // 構え突入時
    {
        // ★確実切替：優先度でaimVCamへ
        ApplyCameraPriority(aiming:true);
        SnapAimVCamOnce();                                   // ★切替瞬間に一度だけ背後右へスナップ

        // ★基準をリセット（最初にターゲットが確定した時点で決める）
        aimHasBaseline = false;
        aimOffsetDeg   = 0f;
    }

    private void AimOrbitUpdate()                            // 構え中の周回見せ方（180°制限）
    {
        // ターゲットがいない場合：カメラの水平方向に体の向きを合わせておく（見た目安定）
        if (currentAimTarget == null)
        {
            if (cameraFollowTarget)
            {
                Vector3 flatForward = cameraFollowTarget.forward; // カメラ前
                flatForward.y = 0f;                               // 水平成分のみ
                if (flatForward.sqrMagnitude > 0.0001f)
                {
                    Quaternion toRot = Quaternion.LookRotation(flatForward);
                    transform.rotation = Quaternion.Slerp(transform.rotation, toRot, rotationSpeed * Time.deltaTime);
                }
            }
            return;
        }

        // ★基準ベクトル（ターゲット→プレイヤーの水平）を初回確定
        if (!aimHasBaseline)
        {
            Vector3 baseDir = transform.position - currentAimTarget.position; // 目標→自分
            baseDir.y = 0f;
            if (baseDir.sqrMagnitude < 0.0001f) baseDir = transform.forward;  // 保険
            aimBaseDirFlat = baseDir.normalized;                               // 正規化して保持
            aimHasBaseline = true;
            aimOffsetDeg   = 0f;                                               // 角オフセット初期化
        }

        // 左スティックのX入力で角オフセットを加算（度/秒）
        float turnX = inputVector.x; // -1〜1
        if (Mathf.Abs(turnX) > 0.01f)
        {
            aimOffsetDeg += turnX * orbitTurnSpeed * Time.deltaTime;          // 加算
            aimOffsetDeg  = Mathf.Clamp(aimOffsetDeg, -orbitHalfSpanDeg, +orbitHalfSpanDeg); // ★180°制限（±90°）
        }

        // ★位置を自前で再計算（基準ベクトルを回転）
        Vector3 center = currentAimTarget.position;                            // 回転中心
        Vector3 dir    = Quaternion.AngleAxis(aimOffsetDeg, Vector3.up) * aimBaseDirFlat; // 回転後の方向

        // ★変更：半径の扱いを“外へ押すのみ”にする（ワープ防止）
        Vector3 flat = transform.position - center;                            // 現在の水平差分
        flat.y = 0f;                                                           // 水平のみ
        float currentRadius = flat.magnitude;                                  // 現在の半径
        float desiredRadius = orbitDistance;                                   // 望ましい半径（安全半径）
        float useRadius = orbitOnlyPushOut ? Mathf.Max(currentRadius, desiredRadius) // 近ければ押し出す／遠ければ維持
                                           : desiredRadius;                    // （引き寄せない）

        Vector3 newPos = center + dir.normalized * (useRadius > 0.001f ? useRadius : desiredRadius); // 新しい位置
        newPos.y = transform.position.y;                                       // 高さは現状維持（必要に応じて接地補正）
        transform.position = newPos;                                           // 位置を更新

        // 常にターゲットの方向を向く（見た目を綺麗に保つ）
        Vector3 lookDir = center - transform.position;                         // ターゲットへの方向
        lookDir.y = 0f;                                                        // 水平のみ
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion toRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, toRot, rotationSpeed * Time.deltaTime);
        }
    }

    // =======================
    // ★追加：カメラ切替ユーティリティ
    // =======================

    private void ApplyCameraPriority(bool aiming)           // 優先度で確実に切替
    {
        if (useAimVCam && aimVCam != null)
        {
            aimVCam.Priority = aiming ? aimVCamPriorityAim : priorityInactive;
            aimVCam.PreviousStateIsValid = false;           // 履歴破棄で“今の設定”に即スナップ
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
        // 3rdPersonFollow/Transposer は Follow/LookAt の姿勢に追従するため、
        // ここでは毎フレーム上書き不要。優先度切替＋スナップのみで安定させる。
    }

    private void SnapFreeLookOnce()                         // FreeLookへ戻す時に一度だけYaw合わせ
    {
        if (freeLook == null) return;
        float yawDeg = Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg; // プレイヤーYaw
        freeLook.m_XAxis.Value = yawDeg;                    // X軸を1回だけ合わせる（ぐるぐる防止）
        // 高さ（Y）はFreeLookの現状値のままでOK
    }

    private void ConfigureAimVCam()                          // 右肩VCamの各種オフセットを適用
    {
        if (aimVCam == null) return;

        // 3rdPersonFollow が付いていれば優先して使う（肩越し視点）
        var tpf = aimVCam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        if (tpf != null)
        {
            tpf.CameraSide   = Mathf.Clamp01(aimCameraSide);               // 1=右肩
            tpf.ShoulderOffset = new Vector3(aimRightOffset, aimUpOffset, 0f);
            tpf.CameraDistance     = Mathf.Max(0.1f, aimBackDistance);
            tpf.Damping      = new Vector3(0.05f, 0.05f, 0.1f);            // わずかに残す程度
            return;
        }

        // 無ければ Transposer で代用（FollowOffsetで右・上・後ろを作る）
        var transposer = aimVCam.GetCinemachineComponent<CinemachineTransposer>();
        if (transposer != null)
        {
            transposer.m_FollowOffset = new Vector3(aimRightOffset, aimUpOffset, -Mathf.Abs(aimBackDistance));
            transposer.m_XDamping = transposer.m_YDamping = transposer.m_ZDamping = 0.05f;
        }
    }

    // =======================
    // ★追加：Aiming連携用のAPI（AimDetectorから呼ばれる）
    // =======================

    public void SetCurrentAimTarget(Transform t)                   // AimDetectorが毎フレーム呼ぶ想定（null可）
    {
        // ターゲットが切り替わったら基準を取り直す
        if (currentAimTarget != t)
        {
            aimHasBaseline = false;                                // 次フレームで基準を再確定
            aimOffsetDeg   = 0f;
        }
        currentAimTarget = t;                                      // 現在のターゲットを更新
    }

    public void SetCurrentAimOrbitRadius(float r)                  // AimDetectorが毎フレーム呼ぶ想定
    {
        orbitDistance = Mathf.Max(0.01f, r);                       // 0以下にならないようにクランプ
    }

    // =======================
    // 公開アクセサ
    // =======================

    public bool IsLofted()                                   // 現在Loftedかどうか返す
    {
        return currentShotType == ShotType.Lofted;           // 列挙型をそのまま利用
    }

    public bool IsAiming()                                   // 現在構え中かどうか返す（AimDetector側で参照）
    {
        return isAiming;                                     // フラグをそのまま返す
    }

    // =======================
    // ★追加：構え可否（距離ゲート＋ターゲット存在）
    // =======================
    private bool CanEnterAim()                               // ターゲットが近くにないと構え不可
    {
        // AimDetector が割当済みなら、その現在ターゲットの有無と距離で判定
        if (aimDetector != null)
        {
            var t = aimDetector.CurrentTarget;               // 現在の最寄りターゲット
            if (t == null) return false;                     // いなければ構え不可

            // 水平距離（XZ）だけ見る：上下差で誤判定しないため
            float d = HorizontalDistanceXZ(transform.position, t.position); // ★追加
            return d <= Mathf.Max(0.01f, aimEnterMaxDistance); // 許容距離内なら構えOK
        }
        // AimDetector未割当なら、保険としてfalseにしておく方が安全
        return false;                                        // 検出無しでの構えは不可
    }

    // ★追加：水平距離だけを計算する小ヘルパー
    private float HorizontalDistanceXZ(Vector3 a, Vector3 b) // 
    {
        a.y = 0f; b.y = 0f;                                   // 高さ無視
        return Vector3.Distance(a, b);                        // XZ平面距離
    }
    
    // ① フィールド（好きなセクションの末尾でOK）
    [Header("スコア")]       // ★追加：見出し
    public int score = 0; // ★現在のスコア

    public System.Action<int,int> OnScored;             // (得点, 合計)

// ② メソッド（クラス内のどこでもOK）
    public void AddScore(int points)                    // （HoleGoalから呼ばれる）
    {
        score += Mathf.Max(0, points);                         // マイナスは防止（必要なら許可に変えてOK）
        Debug.Log($"[Score] {name} +{points}点 → 合計 {score}点"); // デバッグ
        OnScored?.Invoke(points, score);                       // (任意)UI更新などに使えるイベント
    }
}
