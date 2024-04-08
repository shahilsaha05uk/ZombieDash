using UnityEngine;
using System.Collections;

/**
 * 
 * @author 	Gianluca Montaresi
 * @date 	04/01/2016
 * @info 	mntglc@gmail.com
 * 
*/

[ExecuteInEditMode]
public class IndicatorController : MonoBehaviour {

	public enum IndicatorVelocity{
		Immediatly = 0,
		Fast = 1,
		Medium = 2,
		Slow = 3
	}

	[SerializeField]
	IndicatorVelocity _velocity = IndicatorVelocity.Immediatly;
	public IndicatorVelocity Velocity{
		get{	return _velocity;	}
		set{	_velocity = value;	}
	}

	[SerializeField]
	protected float val01 = 0F;
	public virtual float Percent{
		get{	return val01;		}
		set{
			throw new System.NotImplementedException();
		}
	}

	public virtual void Start () {
		//	Debug.Log(name+" Start()");
	}
	
//	public virtual void Shake() {
//	}

}
