using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterHeadLook : MonoBehaviour
{
    CharacterBehaviour behaviour;
    public Transform target;
    public float headWeight;
    public float bodyWeight;
    public Animator animator;

    Vector3 Offset = new Vector3(0,.5f,0);

     float HeadWeight;
     float BodyWeight;

    // Start is called before the first frame update
    void Start()
    {
        behaviour = GetComponentInParent<CharacterBehaviour>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (behaviour.target != null)
            target = behaviour.target.transform;
        else
            target = null;


        if (target != null)
        {
            BodyWeight = Mathf.Lerp(BodyWeight, bodyWeight, Time.deltaTime*1);
            HeadWeight = Mathf.Lerp(HeadWeight, headWeight, Time.deltaTime*1);
        }
        else
        {
            BodyWeight = Mathf.Lerp(BodyWeight, 0, Time.deltaTime*3);
            HeadWeight = Mathf.Lerp(HeadWeight, 0, Time.deltaTime*3.5f);
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (target != null)
        {
            animator.SetLookAtPosition(target.position+ Offset);
            animator.SetLookAtWeight(1, BodyWeight, HeadWeight);
        }
        else
        {
            animator.SetLookAtPosition(behaviour.transform.forward+ Offset + new Vector3(0,0,2));
            animator.SetLookAtWeight(1, BodyWeight, HeadWeight);
        }
    }
}
