using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackAreaActivator : MonoBehaviour {
	Collider[] attackAreaColliders; // 攻撃判定コライダーの配列

	public AudioClip attackSeClip;
	AudioSource attackSeAudio;

	void Start()
	{
		// 子供のGameObjectからAttackAreaスクリプトがついているGameObjectを探す
		AttackArea[] attackAreas = GetComponentsInChildren<AttackArea>();
		attackAreaColliders = new Collider[attackAreas.Length];
		
		for (int attackAreaCnt = 0; attackAreaCnt < attackAreas.Length; attackAreaCnt++) {
			// AttackAreaスクリプトがついているGameObjectのコライダーを配列に格納する
			attackAreaColliders[attackAreaCnt] = attackAreas[attackAreaCnt].GetComponent<Collider>();
			attackAreaColliders[attackAreaCnt].enabled = false; // 初期はfalseにしておく
		}

		// オーディオの初期化
		attackSeAudio = gameObject.AddComponent<AudioSource>();
		attackSeAudio.clip = attackSeClip;
		attackSeAudio.loop = false;
	}

	// アニメーションイベントのStartAttackHitを受け取ってコライダーを有効にする
	void StartAttackHit()
	{
		foreach (Collider attackAreaCollider in attackAreaColliders)
			attackAreaCollider.enabled = true;

		// オーディオ再生
		attackSeAudio.Play();
	}
	
	// アニメーションイベントのEndAttackHitを受け取ってコライダーを無効にする
	void EndAttackHit()
	{
		foreach (Collider attackAreaCollider in attackAreaColliders)
			attackAreaCollider.enabled = false;
	}
}
