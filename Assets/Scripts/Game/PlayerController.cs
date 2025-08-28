using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = System.Diagnostics.Debug;

public class PlayerController : MonoBehaviour
{
    private static readonly int Walk    = Animator.StringToHash("walk");
    private static readonly int Idle    = Animator.StringToHash("idle");
    private static readonly int Jump    = Animator.StringToHash("jump");
    private static readonly int Shot    = Animator.StringToHash("Shot");


    private enum AnimState
    {
        Idle,
        Walk,
        Jump,
        Stance,
        Shot,
    }
    
    private                  float      horizontalInput, verticalInput;
    //[SerializeField] private PlayerData playerData;
    [SerializeField]         float      walkSpeed = 5f; // 歩くスピード
    private                  Rigidbody  playerRb;
    public                   Animator   _animator;
    private                  bool       walkInput = false;
    
    public           Transform groundCheck;              // 足元に置く空オブジェクト
    public           float     groundCheckRadius = 0.2f; // 判定の球の大きさ
    public           LayerMask groundLayer;              // 地面のレイヤー
    public           bool      onGround = true;

    private const float RotateSpeed = 300f;
    
    private                  AnimState            animState = AnimState.Idle;
    private                  bool                 isJumping = false;
    private                  bool                 isShot    = false;
    private bool Stance = false;
    [SerializeField] private InputActionReference _hold;
    private                  InputAction          _holdAction;
    
    InputAction       move;

    void Start()
    {
        if (_hold == null) return;

        _holdAction = _hold.action;
        
        // 入力を受け取るためには必ず有効化する必要がある
        _holdAction.Enable();
        
        playerRb = GetComponent<Rigidbody>();

        var playerInput = GetComponent<PlayerInput>();
        move = playerInput.actions["Move"];
    }

    void Update()
    {
        // 入力の取得
        var inputMoveAxis = move.ReadValue<Vector2>();
        horizontalInput = inputMoveAxis.x;
        verticalInput   = inputMoveAxis.y;
        //CheckSphereで接地判定する
        onGround = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
            
        // 移動入力の有無をチェック
        if (onGround)
        {
            walkInput = (horizontalInput != 0.0f || verticalInput != 0.0f);
        }

        if (isJumping is false)
        {
            if (walkInput && animState is not AnimState.Walk)
            {
                animState = AnimState.Walk;
                _animator.SetTrigger(Walk);
            }
            else if (walkInput is false && animState is not AnimState.Idle)
            {
                animState = AnimState.Idle;
                _animator.SetTrigger(Idle);
            }
        }
        if (_holdAction == null) return;
        {
            Stance = true;
        }
    }


    void FixedUpdate()
    {
        // カメラの向きを考慮して移動方向を計算
        Debug.Assert(Camera.main != null, "Camera.main != null");
        Vector3 cameraForward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 moveForward = cameraForward * verticalInput + Camera.main.transform.right * horizontalInput;

        // 速度をセット（ジャンプ中もYの速度はそのまま）
        playerRb.velocity = moveForward * walkSpeed + new Vector3(0, playerRb.velocity.y, 0);

        // キャラクターを移動方向に回転させる
        if (moveForward != Vector3.zero)
        {
            Quaternion from = transform.rotation;
            Quaternion to = Quaternion.LookRotation(moveForward);
            transform.rotation = Quaternion.RotateTowards(from, to, RotateSpeed * Time.deltaTime);
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        // 新InputSystem用：移動入力はUpdateで取得しているので空でOK
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        // ジャンプが押された & 地面にいるときだけ
        if (context.started && onGround && isJumping is false)
        {
            _animator.SetTrigger(Jump);
            walkInput = false;                    // 移動停止
            onGround = false;                     // 空中状態に
            isJumping = true;
        }
    }
    
    public void ResetJumping()
    {
        isJumping = false;
    }

    public void JumpPower()
    {
        playerRb.velocity = Vector3.up * 5f; // ジャンプ力
    }
    
    /*
    //ショット
    private void OnEnable()
    {
        // ボタンが押されたときの処理を登録
        _hold.action.started += OnAimStarted;
        
        // ボタンを離したときの処理を登録
        _hold.action.canceled += OnAimCanceled;

        // アクションを有効化
        _hold.action.Enable();
    }

    private void OnDisable()
    {
        // イベント登録を解除
        _hold.action.started  -= OnAimStarted;
        _hold.action.canceled -= OnAimCanceled;

        // アクションを無効化
        _hold.action.Disable();
    }

    
    // ボタンが押されたときに呼ばれる
    private void OnAimStarted(InputAction.CallbackContext context)
    {
        if (playerData.ShotAnimationFlag == true)
        {
            ShotAnimation();
        }
        playerData.ShotAnimationFlag = true;
        _animator.SetBool("Stance", true);
        Debug.Log("構える！");
        // 構える処理（アニメーション再生など）
    }

    // ボタンが離されたときに呼ばれる
    private void OnAimCanceled(InputAction.CallbackContext context)
    {
        ShotAnimation();
        Debug.Log("放つ！");
        // 発射処理（弓を放つなど）
    }
    
    private void ShotAnimation()
    {
        if (isShot) return;
        
        isShot = true;
        _animator.SetBool("Stance", false);
        _animator.SetTrigger(Shot);
        StartCoroutine(bollCoolTime());
    }

    private void ShotEnd()
    {
        isShot                       = false;
        playerData.ShotAnimationFlag = false;
        _animator.ResetTrigger(Shot);
    }

    IEnumerator bollCoolTime()
    {
        yield return new WaitForSeconds(0.5f);
        ShotEnd();
    }
    */
    
    //Sceneビューで球の位置が見えるように
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
