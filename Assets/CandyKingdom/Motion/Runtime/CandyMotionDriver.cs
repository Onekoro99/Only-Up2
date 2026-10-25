using UnityEngine;
using OnlyUpPrototype;
namespace CandyKingdom.Motion {
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(PrototypePlayerMotor),typeof(Animator))]
public sealed class CandyMotionDriver:MonoBehaviour {
 [SerializeField,Range(.1f,3f)]
 [Header("モーションの切り替え")]
 [Tooltip("下降開始からLong Fallへ切り替えるまでの時間（秒）です。大きいほど通常の落下姿勢が長く、小さいほど早く長時間落下の姿勢になります。初期値0.65秒です。")]
 float longFallDelay=.65f;
 [SerializeField,Min(.1f)]
 [Tooltip("走りアニメーションの基準速度（m/秒）です。実際の横移動速度をこの値で割って再生倍率を決めます。大きくすると同じ移動速度で遅い再生になり、移動速度自体は変わりません。初期値3.5で、再生倍率は0.65〜1.5に制限されます。")]
 float runReferenceSpeed=3.5f;
 PrototypePlayerMotor motor;Animator animator;
 bool initialized;float fallTime;Vector3 lastPosition;
 public float LongFallDelay {get=>longFallDelay;set=>longFallDelay=Mathf.Max(.1f,value);}
 public float FallTime=>fallTime;
 void Awake(){motor=GetComponent<PrototypePlayerMotor>();animator=GetComponent<Animator>();}
 void Update(){Tick(Time.deltaTime);}
 public void ResetTracking(){initialized=false;fallTime=0;}
 public void Tick(float dt){
  dt=Mathf.Max(0,dt);
  bool ground=motor.IsGrounded;float vertical=motor.VerticalSpeed;
  bool teleported=initialized&&Vector3.Distance(lastPosition,transform.position)>Mathf.Max(2f,(motor.HorizontalVelocity.magnitude+Mathf.Abs(vertical))*Mathf.Max(dt,.001f)*3f);
  if(!initialized||teleported){fallTime=0;initialized=true;}
  if(ground||vertical>0)fallTime=0;else fallTime+=dt;
  animator.SetFloat("VerticalSpeed",vertical);
  animator.SetBool("LongFalling",!ground&&fallTime>=longFallDelay);
  animator.SetFloat("RunRate",Mathf.Clamp(motor.HorizontalVelocity.magnitude/Mathf.Max(.1f,runReferenceSpeed),.65f,1.5f));
  lastPosition=transform.position;
 }
}
}