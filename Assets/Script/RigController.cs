using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class RigController : MonoBehaviour
{
    [SerializeField] private Rig rig;
    [SerializeField] private Transform targetTransform;
    float rigWeight = 1;
    float targetWeight = 0;
    [SerializeField] public float weightChangeSpeed = 2f;
    private Vector3 setPosition;

    private CharacterBehaviour behaviour;

    public bool targetTracking
    {
        get;
        private set;
    } = false;
    private GameObject targetObject;
    private Vector3 trackingOffset;
    public bool isInAction;

    private void Start()
    {
        behaviour = GetComponentInParent<CharacterBehaviour>();
        rig.weight = rigWeight;
        setPosition = transform.position;
    }

    void LateUpdate()
    {
        switch (behaviour.currentState)
        {
            case CharacterState.Idle:
            case CharacterState.Move:
                {
                    targetTracking = true;
                    break;
                }
            case CharacterState.Action:
                {
                    if (behaviour.currentActionData == null)
                        return;
                    isInAction = true;
                    if (behaviour.target == null)
                    {
                        targetTracking = false;
                    }
                    else
                    {
                        if (behaviour.currentActionData.canLookInAction)
                            targetTracking = true;
                        else
                        {
                            targetTracking = false;
                        }
                    }
                    break;
                }
            case CharacterState.KnockBack:
            case CharacterState.KnockBackSmash:
                {
                    targetTracking = false;
                    break;
                }
        }

        if (behaviour.target == null)
        {
            targetWeight = 0f;
            targetTransform.localPosition = Vector3.Lerp(targetTransform.localPosition, new Vector3(0, 0, 3) + trackingOffset, weightChangeSpeed * Time.deltaTime);
        }
        else if (behaviour.OnEvolution)
        {
            targetWeight = 0f;
            targetTracking = false;
            targetTransform.localPosition = Vector3.Lerp(targetTransform.localPosition, new Vector3(0, 0, 3) + trackingOffset, 10 * Time.deltaTime);
        }
        else
        {
            if (isInAction)
            {
                if (targetTracking)
                {
                    targetObject = behaviour.target.gameObject;
                    targetTransform.position = Vector3.Lerp(targetTransform.position, targetObject.transform.position + trackingOffset, 20 * Time.deltaTime);
                    targetWeight = 1f;
                }
                else
                {
                    targetObject = behaviour.target.gameObject;
                    targetTransform.position = Vector3.Lerp(targetTransform.position, targetObject.transform.position + trackingOffset, 20 * Time.deltaTime);
                    targetWeight = 0f;
                }

            }
            else
            {
                if (targetTracking)
                {
                    targetObject = behaviour.target.gameObject;
                    targetTransform.position = Vector3.Lerp(targetTransform.position, targetObject.transform.position + trackingOffset, 20 * Time.deltaTime);
                    targetWeight = 1f;
                }
                else
                {
                    targetObject = behaviour.target.gameObject;
                    targetTransform.position = Vector3.Lerp(targetTransform.position, targetObject.transform.position + trackingOffset, 20 * Time.deltaTime);
                    targetWeight = 0f;
                }
            }
        }

        // Smoothly interpolate rig weight toward the target weight
        rigWeight = Mathf.Lerp(rigWeight, targetWeight, weightChangeSpeed * Time.deltaTime);
        rig.weight = rigWeight;
    }

    public void SetUp(Vector3 offset, float weightSpeed)
    {
        trackingOffset = offset;
        weightChangeSpeed = weightSpeed;
    }

    public void SetRigWeight(float weight)
    {
        rigWeight = weight;
    }
}
