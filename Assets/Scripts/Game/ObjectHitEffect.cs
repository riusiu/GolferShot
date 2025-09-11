using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectHitEffect : MonoBehaviour
{
    [SerializeField]private ParticleSystem particle;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            // パーティクルシステムのインスタンスを生成する。
            ParticleSystem newParticle = Instantiate(particle);
            // パーティクルの発生場所をこのスクリプトをアタッチしているGameObjectの場所にする。
            newParticle.transform.position = this.transform.position;
            // パーティクルを発生させる。
            newParticle.Play();
            // インスタンス化したパーティクルシステムのGameObjectを5秒後に削除する。(任意)
        }
    }
}
