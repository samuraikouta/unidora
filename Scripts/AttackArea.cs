using System.Collections;
using UnityEngine;

public class AttackArea : MonoBehaviour
{
	CharacterStatus status;

	public AudioClip hitSeClip;
	AudioSource hitSeAudio;

	void Start()
	{
		status = transform.root.GetComponent<CharacterStatus>();

		// オーディをの初期化
		hitSeAudio = gameObject.AddComponent<AudioSource>();
		hitSeAudio.clip = hitSeClip;
		hitSeAudio.loop = false;
	}

	public class AttackInfo
	{
		public int attackPower; // この攻撃の攻撃力
		public Transform attacker; // 攻撃者
	}

	// 攻撃者を取得する
	AttackInfo GetAttackInfo()
	{
		AttackInfo attackInfo = new AttackInfo();
		// 攻撃力の計算
		attackInfo.attackPower = status.Power;
		// 攻撃強化中
		if (status.powerBoost){
			attackInfo.attackPower += attackInfo.attackPower;
		}
			
		attackInfo.attacker = transform.root;

		return attackInfo;
	}

	// 当たった
	void OnTriggerEnter(Collider other)
	{
		// 攻撃が当たった相手のDamageメッセージをおくる　
		other.SendMessage("Damage", GetAttackInfo());
		// 攻撃した対象を保存
		status.lastAttackTarget = other.transform.root.gameObject;
		// オーディオ再生
		hitSeAudio.Play();
	}

	// 攻撃判定を有効にする
	void OnAttack()
	{
		if (GetComponent<Collider>() != null)
		{
			GetComponent<Collider>().enabled = true;
		}
	}

	// 攻撃判定を無効にする
	void OnAttackTermination()
	{
		if (GetComponent<Collider>() != null)
		{
			GetComponent<Collider>().enabled = false;
		}
	}
}
