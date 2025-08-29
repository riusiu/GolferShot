using UnityEngine;                          // Unityの基本機能
using UnityEngine.InputSystem;              // 新Input Systemを使う
using Cinemachine;                          // FreeLook参照（このスクリプトでは参照のみ）

[RequireComponent(typeof(Rigidbody))]       // Rigidbody必須
[RequireComponent(typeof(Animator))]        // Animator必須
public class PlayerController : MonoBehaviour
{
    [Header("移動設定")]
    public float moveSpeed = 5f;            // 地上移動スピード
    public float rotationSpeed = 10f;       // 旋回（見た目の回転）速度

    [Header("ジャンプ設定")]
    public float jumpForce = 5f;            // ジャンプ力（Impulse）
    public LayerMask groundMask;            // 地面レイヤー
    public float groundCheckRadius = 0.25f; // 接地判定の球半径
    public float groundCheckDistance = 0.3f;// 真下に飛ばす距離

    [Header("カメラ")]
    public Transform cameraFollowTarget;    // カメラ基準の向き（FreeLookのFollow/LookAtと同じでOK）

    private Rigidbody rb;                   // 物理本体
    private Animator animator;              // アニメーター
    private PlayerInput playerInput;        // 入力ルーター
    private InputAction moveAction;         // Move（Vector2）
    private InputAction jumpAction;         // Jump（Button）

    private Vector2 inputVector;            // 移動入力
    private bool isGrounded = false;        // 接地中フラグ（物理で判定）
    private bool canJump = true;            // ジャンプ可能フラグ（アニメイベントで管理）

    void Awake()                            // 初期取得
    {
        rb = GetComponent<Rigidbody>();     // Rigidbody参照
        animator = GetComponent<Animator>();// Animator参照
        playerInput = GetComponent<PlayerInput>(); // PlayerInput参照

        moveAction = playerInput.actions["Move"];  // InputActionのMoveを取得
        jumpAction = playerInput.actions["Jump"];  // InputActionのJumpを取得
    }

    void Update()                           // 入力とアニメ更新
    {
        inputVector = moveAction.ReadValue<Vector2>(); // 移動入力を読む

        UpdateGroundedState();              // 物理で接地判定を更新

        float speedPercent = inputVector.magnitude; // 0〜1の入力強度
        animator.SetFloat("speed", speedPercent);   // BlendTree用（0/0.3/1で区切る前提）
        animator.SetBool("IsGrounded", isGrounded); // 任意：アニメ側でも使える

        // ジャンプボタンが押され、接地していて、かつロックが外れている時だけジャンプ
        if (jumpAction.triggered && isGrounded && canJump)
        {
            DoJump();                      // 実ジャンプ
        }
    }

    void FixedUpdate()                      // 物理フレームで移動
    {
        Move();                             // Rigidbody.velocity を更新
    }

    private void Move()                     // カメラ基準の移動
    {
        // 入力がほぼゼロなら水平速度を止めて終了
        if (inputVector.sqrMagnitude < 0.0001f)
        {
            Vector3 v = rb.velocity;       // 現在速度
            rb.velocity = new Vector3(0f, v.y, 0f); // 水平を止めて垂直だけ維持
            return;
        }

        // 入力（Xが左右、Yが前後）→ カメラ向きでワールドへ
        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y); // ローカル入力
        float camY = cameraFollowTarget ? cameraFollowTarget.eulerAngles.y : transform.eulerAngles.y; // 基準角
        moveDir = Quaternion.Euler(0f, camY, 0f) * moveDir; // カメラ基準に回す
        moveDir.Normalize();                                // 正規化で一定速度

        // 目標速度（水平）をセット、縦は既存維持（重力やジャンプ）
        Vector3 targetVel = moveDir * moveSpeed;            // 目標水平速度
        targetVel.y = rb.velocity.y;                        // 垂直は維持
        rb.velocity = targetVel;                            // 速度を適用

        // 見た目の向きも移動方向へ
        Quaternion toRot = Quaternion.LookRotation(moveDir); // 進行方向を向く回転
        transform.rotation = Quaternion.Slerp(               // スムーズに補間
            transform.rotation, toRot, rotationSpeed * Time.deltaTime);
    }

    private void UpdateGroundedState()       // 接地判定（SphereCast）
    {
        Vector3 origin = transform.position + Vector3.up * 0.05f; // 少し上から始点
        Ray ray = new Ray(origin, Vector3.down);                   // 下向きのレイ
        bool hit = Physics.SphereCast(                             // 球で当たりを安定判定
            ray,                                                   // レイ
            groundCheckRadius,                                     // 半径
            out RaycastHit hitInfo,                                // ヒット情報不要でも受け取る
            groundCheckDistance,                                   // 距離
            groundMask,                                            // 対象レイヤー
            QueryTriggerInteraction.Ignore);                       // トリガー除外

        isGrounded = hit;                                          // 結果を反映
    }

    private void DoJump()                    // 実ジャンプ処理
    {
        // 多重ジャンプ防止：ここでロック
        canJump = false;                     // 着地アニメの終わりまで不可

        // 縦速度を一旦0にしてからImpulse（前フレームの落下速度の影響を消す）
        Vector3 v = rb.velocity;             // 現在速度
        v.y = 0f;                            // 縦だけリセット
        rb.velocity = v;                     // 反映

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse); // 上向き衝撃力

        animator.SetTrigger("jump");         // ジャンプアニメへ遷移（Jumpトリガーを想定）
    }

    // === Animation Event から呼ぶメソッド（ジャンプアニメの終端で設定） ===
    public void OnJumpEnd()                  // クリップ末尾でイベントを打つ
    {
        // 接地していればロック解除（空中で終端が来ても解除しない）
        UpdateGroundedState();               // 念のため最新の接地を確認
        if (isGrounded)                      // 地面にいるなら
        {
            canJump = true;                  // 次のジャンプを許可
        }
        // ※ まだ空中なら、着地した次のフレームで isGrounded が true になるので
        //    そのときの Idle/Run アニメ末尾でもう一度 OnJumpEnd を打つなら解除できます。
        //    もしくは、着地トリガー用のStateに別イベントを置いて同じ関数を呼んでもOK。
    }

    private void OnDrawGizmosSelected()      // デバッグ可視化（エディタだけ）
    {
        Gizmos.color = Color.red;         // 色
        Vector3 origin = transform.position + Vector3.up * 0.05f; // 始点
        // 判定球の位置をワイヤー表示
        Gizmos.DrawWireSphere(origin + Vector3.down * groundCheckDistance, groundCheckRadius);
    }
}
