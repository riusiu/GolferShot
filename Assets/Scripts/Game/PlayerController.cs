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

    [Header("カメラ")]
    public Transform cameraFollowTarget;                // カメラ基準の向き

    [Header("ゴルフショット")]
    public float shotPower = 15f;                       // ショットのパワー（Impulse）
    public float shotUpwardForce = 0.3f;                // 山なり用の上向き成分
    public float rayDistance = 3f;                      // ボール検出Rayの距離
    public LayerMask ballMask;                          // ボール用レイヤー

    [Header("クラブ表示")]
    public GameObject loftedClubModel;                  // 山なりクラブの見た目
    public GameObject straightClubModel;                // 直線クラブの見た目

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
    private GameObject currentBall;                     // 対象ボール

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
    }

    void Update()
    {
        inputVector = moveAction.ReadValue<Vector2>();  // 移動入力を読む
        UpdateGroundedState();                          // 接地更新

        float speedPercent = inputVector.magnitude;     // 入力強度0〜1
        animator.SetFloat("speed", speedPercent);       // アニメ用速度
        animator.SetBool("IsGrounded", isGrounded);     // アニメ用接地

        // === 構え開始：Shotを押し始めた瞬間（長押し中に維持） ===
        if (shotAction.IsPressed() && !isAiming && !isShooting) // まだ構えておらず、スイング中でもない
        {
            isAiming = true;                            // 構えフラグON
            animator.SetBool("form", true);             // formアニメON（あなたのパラメ名に合わせて"form"）
            FindBall();                                 // 目の前ボール検出
            RefreshClubVisual();                        // クラブ表示（構え中ON）
        }

        // === 構え終了→スイング開始：Shotを離した瞬間 ===
        if (isAiming && shotAction.WasReleasedThisFrame()) // 構え中に指を離した
        {
            isAiming = false;                           // 構えフラグOFF（formアニメは終了方向へ）
            animator.SetBool("form", false);            // formをOFF（遷移）
            isShooting = true;                          // スイングフラグON（移動ロック継続）
            animator.SetTrigger("shot");                // Shotアニメを開始（Animatorに"shot"トリガーを用意）
            RefreshClubVisual();                        // 表示継続（isShooting=trueのため表示維持）
            // ※ 物理的な発射はアニメのインパクトフレームでAnimation Eventから呼ぶ（下のAnimEvent_ShotFire）
        }

        // === ジャンプ（構え/スイング中は不可） ===
        if (jumpAction.triggered && isGrounded && canJump && !IsActionLocked()) // ロック中でない
        {
            DoJump();                                   // 実ジャンプ
        }

        // === グラブ切替（L1/R1：交互トグル） ===
        if (switchShotTypeAction.triggered)            // 押された瞬間
        {
            currentShotType =                          // 山なり↔直線
                (currentShotType == ShotType.Lofted) ? ShotType.Straight : ShotType.Lofted;

            Debug.Log("グラブ切替: " + currentShotType);
            RefreshClubVisual();                       // 構え/スイング中なら見た目即時反映
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
        Vector3 origin = transform.position + Vector3.up * 0.05f; // 少し上から
        Ray ray = new Ray(origin, Vector3.down);             // 下向きレイ
        bool hit = Physics.SphereCast(                       // 球で安定判定
            ray, groundCheckRadius, out RaycastHit hitInfo,
            groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
        isGrounded = hit;                                    // 保存
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

    private void FindBall()                                  // 目の前のボールをRayで取得
    {
        Vector3 origin = cameraFollowTarget.position;        // カメラ位置
        Vector3 dir = cameraFollowTarget.forward;            // カメラ前
        if (Physics.Raycast(origin, dir, out RaycastHit hit,
                            rayDistance, ballMask))          // ボールにヒット？
        {
            currentBall = hit.collider.gameObject;           // 記憶
            Debug.Log("ボール検出: " + currentBall.name);    // ログ
        }
        else
        {
            currentBall = null;                              // 見つからず
            Debug.Log("ボールが見つかりません");                // ログ
        }
    }

    // =======================
    // アニメーションイベント群
    // =======================

    // ▼ formアニメの終端で呼ぶ（任意）。今回はスイング開始を入力解放で行うため必須ではない。
    public void AnimEvent_FormEnd()                          // formの終端
    {
        // ここでは特別な処理は不要（必要ならフラグ整合を確認）
        // 例：Debug.Log("Form End");
    }

    // ▼ Shotアニメの"インパクト"フレームで呼ぶ：実際にボールへ力を与える
    public void AnimEvent_ShotFire()                         // 発射タイミング
    {
        if (!isShooting) return;                             // スイング中のみ
        if (currentBall == null) return;                     // ボールが無ければ無視

        Rigidbody ballRb = currentBall.GetComponent<Rigidbody>(); // ボールの剛体
        if (ballRb == null) return;                          // 無ければ無視

        ballRb.velocity = Vector3.zero;                      // 速度リセット
        ballRb.angularVelocity = Vector3.zero;               // 回転リセット

        Vector3 dir = cameraFollowTarget.forward;            // カメラ前方向
        dir.y = 0f;                                          // 水平化
        dir.Normalize();                                     // 正規化

        if (currentShotType == ShotType.Lofted)              // 山なりなら
            dir += Vector3.up * shotUpwardForce;             // 上成分を追加

        ballRb.AddForce(dir * shotPower, ForceMode.Impulse); // インパルスで飛ばす
        Debug.Log("ショット実行(AnimEvent): " + currentShotType);
    }

    // ▼ Shotアニメの終端で呼ぶ：ここで移動解禁＆クラブ非表示
    public void AnimEvent_ShotEnd()                          // スイング完了
    {
        isShooting = false;                                  // スイング終了
        RefreshClubVisual();                                 // 表示更新（非表示へ）
        // これで IsActionLocked() が false になり、移動・ジャンプが解禁される
        // 例：Debug.Log("Shot End");
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
}
