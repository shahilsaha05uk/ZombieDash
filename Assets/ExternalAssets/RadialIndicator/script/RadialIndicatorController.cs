using UnityEngine;
using System.Collections;

/**
 * 
 * @author 	Gianluca Montaresi
 * @date 	28/12/2015
 * @info 	mntglc@gmail.com
 * 
*/

//[ExecuteInEditMode]
public class RadialIndicatorController : IndicatorController {

	const string GameObjectName_Base = "base";
	const string GameObjectName_BaseNeedle = "center";
	const string GameObjectName_Alarm = "mark";
	const string GameObjectName_Needle = "needle";
	const string GameObjectName_NeedleZero = "zero";
	const string GameObjectName_Nick = "nick";

	public enum RadialIndicatorTurn{
		Clockwise = 0,
		Counterclockwise = 1
	}
	GameObject baseGameObject = null;
	GameObject baseNeedleGameObject = null;
	GameObject alarmGameObject = null;
	GameObject nickGameObject = null;
	Transform needleTransform = null;
	Transform locatorTransform = null;

	public override void Start () {
		base.Start();
		Transform[] transforms = GetComponentsInChildren<Transform>();
		for(int i=0; i<transforms.Length; i++)
		{
			if(transforms[i].gameObject.name.CompareTo(GameObjectName_Needle) == 0)
			{
				needleTransform = transforms[i];
			}
			else if(transforms[i].gameObject.name.CompareTo(GameObjectName_NeedleZero) == 0)
			{
				locatorTransform = transforms[i];
			}
			else if(transforms[i].gameObject.name.CompareTo(GameObjectName_Base) == 0)
			{
				baseGameObject = transforms[i].gameObject;
			}
			else if(transforms[i].gameObject.name.CompareTo(GameObjectName_BaseNeedle) == 0)
			{
				baseNeedleGameObject = transforms[i].gameObject;
			}
			else if(transforms[i].gameObject.name.CompareTo(GameObjectName_Alarm) == 0)
			{
				alarmGameObject = transforms[i].gameObject;
			}
			else if(transforms[i].gameObject.name.CompareTo(GameObjectName_Nick) == 0)
			{
				nickGameObject = transforms[i].gameObject;
			}
		}
		isRun = false;
		Turn = _turn;
		Base = _base;
		Range = _range;
		Percent = val01;
	}
		
#region Properties
	[SerializeField]
	float _base = 0F;
	public float Base{
		get{	return _base;		}
		set{
			_base = value;
			if(locatorTransform)
				locatorTransform.eulerAngles = new Vector3(locatorTransform.eulerAngles.x, locatorTransform.eulerAngles.y, _base);
			Percent = 0;
		}
	}

	[SerializeField]
	float _range = 90F;
	public float Range{
		get{	return _range;		}
		set{	_range = value;		}
	}

	[SerializeField]
	RadialIndicatorTurn _turn = RadialIndicatorTurn.Clockwise;
	public RadialIndicatorTurn Turn{
		get{	return _turn;		}
		set{
			_turn = value;
			if(locatorTransform != null)
			{
				switch(_turn)
				{
				case RadialIndicatorTurn.Clockwise:
					locatorTransform.eulerAngles = new Vector3(locatorTransform.eulerAngles.x, 180F, -locatorTransform.eulerAngles.z);
					break;
				case RadialIndicatorTurn.Counterclockwise:
					locatorTransform.eulerAngles = new Vector3(locatorTransform.eulerAngles.x, 0F, -locatorTransform.eulerAngles.z);
					break;
				}
			}
			Percent = 0;
		}
	}
		
	float val360 = 0F;
	public override float Percent{
		set{
			val01 = value;
			val360 = Range * val01;
			if(needleTransform != null)
				Set();
		}
	}

	public float rotation{
		get{	return (needleTransform != null) ? needleTransform.eulerAngles.z : 0F;			}
	}

	public float localRotation{
		get{	return (needleTransform != null) ?  needleTransform.localEulerAngles.z : 0F;	}
	}

	// ------------ BASE ------------
	SpriteRenderer baseSR = null;
	public Color ColorBase{
		get{
			Color c = Color.white;
			if (baseGameObject == null)
				return c;
			if(baseSR == null)
				baseSR = baseGameObject.GetComponent<SpriteRenderer> ();
			if (baseSR == null)
				return c;
			return baseSR.color;
		}
		set{
			if (baseGameObject == null)
				return;
			if(baseSR == null)
				baseSR = baseGameObject.GetComponent<SpriteRenderer> ();
			if (baseSR != null)
				baseSR.color = value;
		}
	}

	public Sprite SpriteBase{
		get{
			if (baseGameObject == null)
				return null;
			if(baseSR == null)
				baseSR = baseGameObject.GetComponent<SpriteRenderer> ();
			if (baseSR == null)
				return null;
			return baseSR.sprite;
		}
		set{
			if (baseGameObject == null)
				return;
			if(baseSR == null)
				baseSR = baseGameObject.GetComponent<SpriteRenderer> ();
			if (baseSR != null)
				baseSR.sprite = value;
		}
	}

	// ------------ BASE NEEDLE ------------
	SpriteRenderer baseNeedleSR = null;
	public Color ColorBaseNeedle{
		get{
			Color c = Color.white;
			if (baseNeedleGameObject == null)
				return c;
			if(baseNeedleSR == null)
				baseNeedleSR = baseNeedleGameObject.GetComponent<SpriteRenderer> ();
			if (baseNeedleSR == null)
				return c;
			return baseNeedleSR.color;
		}
		set{
			if (baseNeedleGameObject == null)
				return;
			if(baseNeedleSR == null)
				baseNeedleSR = baseNeedleGameObject.GetComponent<SpriteRenderer> ();
			if (baseNeedleSR != null)
				baseNeedleSR.color = value;
		}
	}

	public Sprite SpriteBaseNeedle{
		get{
			if (baseNeedleGameObject == null)
				return null;
			if(baseNeedleSR == null)
				baseNeedleSR = baseNeedleGameObject.GetComponent<SpriteRenderer> ();
			if (baseNeedleSR == null)
				return null;
			return baseNeedleSR.sprite;
		}
		set{
			if (baseNeedleGameObject == null)
				return;
			if(baseNeedleSR == null)
				baseNeedleSR = baseNeedleGameObject.GetComponent<SpriteRenderer> ();
			if (baseNeedleSR != null)
				baseNeedleSR.sprite = value;
		}
	}

	// ------------ ALARM ------------
	SpriteRenderer alarmSR = null;
	public Color ColorAlarm{
		get{
			Color c = Color.white;
			if (alarmGameObject == null)
				return c;
			if(alarmSR == null)
				alarmSR = alarmGameObject.GetComponent<SpriteRenderer> ();
			if (alarmSR == null)
				return c;
			return alarmSR.color;
		}
		set{
			if (alarmGameObject == null)
				return;
			if(alarmSR == null)
				alarmSR = alarmGameObject.GetComponent<SpriteRenderer> ();
			if (alarmSR != null)
				alarmSR.color = value;
		}
	}

	public Sprite SpriteAlarm{
		get{
			if (alarmGameObject == null)
				return null;
			if(alarmSR == null)
				alarmSR = alarmGameObject.GetComponent<SpriteRenderer> ();
			if (alarmSR == null)
				return null;
			return alarmSR.sprite;
		}
		set{
			if (alarmGameObject == null)
				return;
			if(alarmSR == null)
				alarmSR = alarmGameObject.GetComponent<SpriteRenderer> ();
			if (alarmSR != null)
				alarmSR.sprite = value;
		}
	}

	// ------------ NEEDLE ------------
	SpriteRenderer needleSR = null;
	public Color ColorNeedle{
		get{
			Color c = Color.white;
			if (needleTransform == null)
				return c;
			if(needleSR == null)
				needleSR = needleTransform.gameObject.GetComponent<SpriteRenderer> ();
			if (needleSR == null)
				return c;
			return needleSR.color;
		}
		set{
			if (needleTransform == null)
				return;
			if(needleSR == null)
				needleSR = needleTransform.gameObject.GetComponent<SpriteRenderer> ();
			if (needleSR != null)
				needleSR.color = value;
		}
	}

	public Sprite SpriteNeedle{
		get{
			if (needleTransform == null)
				return null;
			if(needleSR == null)
				needleSR = needleTransform.gameObject.GetComponent<SpriteRenderer> ();
			if (needleSR == null)
				return null;
			return needleSR.sprite;
		}
		set{
			if (needleTransform == null)
				return;
			if(needleSR == null)
				needleSR = needleTransform.gameObject.GetComponent<SpriteRenderer> ();
			if (needleSR != null)
				needleSR.sprite = value;
		}
	}


	// ------------ NICK ------------
	SpriteRenderer nickSR = null;
	public Color ColorNick{
		get{
			Color c = Color.white;
			if (nickGameObject == null)
				return c;
			if(nickSR == null)
				nickSR = nickGameObject.GetComponent<SpriteRenderer> ();
			if (nickSR == null)
				return c;
			return nickSR.color;
		}
		set{
			if (needleTransform == null)
				return;
			if(nickSR == null)
				nickSR = nickGameObject.GetComponent<SpriteRenderer> ();
			if (nickSR != null)
				nickSR.color = value;
		}
	}

	public Sprite SpriteNick{
		get{
			if (nickGameObject == null)
				return null;
			if(nickSR == null)
				nickSR = nickGameObject.GetComponent<SpriteRenderer> ();
			if (nickSR == null)
				return null;
			return nickSR.sprite;
		}
		set{
			if (needleTransform == null)
				return;
			if(nickSR == null)
				nickSR = nickGameObject.GetComponent<SpriteRenderer> ();
			if (nickSR != null)
				nickSR.sprite = value;
		}
	}
#endregion

#region Set
	public bool isRun = false;

	void Set() {
		if(Velocity == IndicatorVelocity.Immediatly)
		{ 	
			StopAllCoroutines();
			isRun = false;
			needleTransform.localEulerAngles = new Vector3(0F, needleTransform.localEulerAngles.y, val360);
		}
		else{
			if(!isRun)
			{
				isRun = true;
				StartCoroutine("Go");
			}
		}
	}

	IEnumerator Go() {
		float t = 0F, v = 0F;
		switch(Velocity)
		{
		case IndicatorVelocity.Fast: 		v = 0.1F; 		break;
		case IndicatorVelocity.Medium: 		v = 0.01F; 		break;
		case IndicatorVelocity.Slow: 		v = 0.001F; 	break;
		}
		while(Mathf.Abs(needleTransform.localEulerAngles.z - val360) >= 0.001F)
		{
			t += Time.deltaTime * v;
			needleTransform.localEulerAngles = Vector3.Lerp(
				needleTransform.localEulerAngles,
				new Vector3(0F, needleTransform.localEulerAngles.y, val360),
				t);
			yield return null;
		}
		isRun = false;
		yield break;
	}

//	public override void Shake() {
//		StopAllCoroutines ();
//		StartCoroutine ("Shaking");
//	}
//	public float _v = 0.1F, _offset = 100F;
//	IEnumerator Shaking() {
////		float t = 0F, v = 0.1F, offset = 100F, newVal = 0F;
////		newVal = needleTransform.localEulerAngles.z + _offset;
////		while(Mathf.Abs(needleTransform.localEulerAngles.z - newVal) >= 0.001F)
////		{
////			Debug.Log ("One ["+Mathf.Abs(needleTransform.localEulerAngles.z - newVal)+"] ["+needleTransform.localEulerAngles.z+"] ["+newVal+"]");
////			t += Time.deltaTime * _v;
////			needleTransform.localEulerAngles = Vector3.Lerp(
////				needleTransform.localEulerAngles,
////				new Vector3(0F, needleTransform.localEulerAngles.y, newVal),
////				t);
////			yield return null;
////		}
////		t = 0F;
////		newVal = needleTransform.localEulerAngles.z - _offset ;
////		while(Mathf.Abs(needleTransform.localEulerAngles.z - newVal) >= 0.001F)
////		{
////			Debug.Log ("Two");
////			t += Time.deltaTime * _v;
////			needleTransform.localEulerAngles = Vector3.Lerp(
////				needleTransform.localEulerAngles,
////				new Vector3(0F, needleTransform.localEulerAngles.y, newVal),
////				t);
////			yield return null;
////		}
////		t = 0F;
////		newVal = needleTransform.localEulerAngles.z + _offset;
////		while(Mathf.Abs(needleTransform.localEulerAngles.z - newVal) >= 0.001F)
////		{
////			Debug.Log ("three");
////			t += Time.deltaTime * _v;
////			needleTransform.localEulerAngles = Vector3.Lerp(
////				needleTransform.localEulerAngles,
////				new Vector3(0F, needleTransform.localEulerAngles.y, newVal),
////				t);
////			yield return null;
////		}
//
//		if (!isRun)
//		{
//			Debug.Log ("dio cane");
//			val360 = val360 + _offset;
//			yield return new WaitForEndOfFrame();
//			while(isRun)
//			{
// 				Debug.Log ("one");
//				yield return null;
//			}
//			val360 = val360 - _offset * 2;
//			yield return new WaitForEndOfFrame();
//			while(isRun)
//			{
//				Debug.Log ("two");
//				yield return null;
//			}
//			val360 = val360 + _offset;
//			yield return new WaitForEndOfFrame();
//			while(isRun)
//			{
//				Debug.Log ("three");
//				yield return null;
//			}
//		}
//		Debug.Log ("porco dio");
//		yield break;
//	}
	#endregion
}
