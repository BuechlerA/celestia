using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Telekinesis : MonoBehaviour
{
    public enum State
    {
        Idle,
        Pushing,
        Pulling,
        Exploding
    }

    public State state = State.Idle;
    
    [Header("References")]
    [SerializeField] Transform baseTrans;
    [SerializeField] Camera cam;
    [SerializeField] Image cursor;
    [SerializeField] AudioSource telekinesisAudio;

    [Header("Settings")]
    [SerializeField] float pullForce = 60;
    [SerializeField] float pushForce = 60;
    [SerializeField] float range = 70;
    [SerializeField] LayerMask detectionLayerMask;
	[SerializeField] float explosiveForce = 200;
	[SerializeField] float explosiveRadius = 10;

	private Transform target;
    private Transform lastTarget;
    private Vector3 targetHitPoint;
    private Rigidbody targetRigidbody;
    private bool targetIsOutsideRange = false;

    private Color CursorColor
    {
        get
        {
            if(state == State.Idle)
                if(target == null)
                    return Color.grey;
                else if(targetIsOutsideRange)
                    return new Color(1, .6f, 0);
                else   
                    return Color.white;
            else 
                return Color.green;
        }
    }

    void TargetDetection()
    {
        var ray = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0));
        RaycastHit hit;

        if(Physics.Raycast(ray, out hit, Mathf.Infinity, detectionLayerMask.value))
        {
            if(hit.rigidbody != null && !hit.rigidbody.isKinematic)
            {
                if(lastTarget != null && lastTarget != hit.transform)
                    ClearTarget();
                    
                target = hit.transform;
                lastTarget = target;
                targetRigidbody = hit.rigidbody;
                targetHitPoint = hit.point;

                if(target.GetComponent<Outline>() == null)
                    target.gameObject.AddComponent(typeof(Outline));
                else
                    target.GetComponent<Outline>().enabled = true;

                if(Vector3.Distance(baseTrans.position, hit.point) > range)
                    targetIsOutsideRange = true;
                else   
                    targetIsOutsideRange = false;
            }
            else
            {
                ClearTarget();
            }
        }
        else
        {
            ClearTarget();
        }
    }

    void ClearTarget()
    {
        if(target != null)
        {
            if(target.GetComponent<Outline>() != null)
                target.GetComponent<Outline>().enabled = false;
        }

        target = null;
        targetRigidbody = null;
        targetIsOutsideRange = false;
    }

    void PullingAndPushing()
    {
        if (target == null || targetIsOutsideRange)
        {
            state = State.Idle;
            return;
        }

        if (!Input.anyKey)
        {
            state = State.Idle;
            return;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            // If both are pressed at the same time, explode at that point
            if (Input.GetKey(KeyCode.E))
            {
                Vector3 explosionPosition = targetHitPoint;
                Collider[] colliders = Physics.OverlapSphere(explosionPosition, explosiveRadius);
                foreach (Collider struck in colliders)
                {
                    Rigidbody rb = struck.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddExplosionForce(explosiveForce, explosionPosition, explosiveRadius, 1.5f);
                    }
                }
                state = State.Exploding;
            }
            else
            {
                targetRigidbody.AddForce((baseTrans.position - targetHitPoint).normalized * pullForce, ForceMode.Acceleration);
                state = State.Pulling;
            }

        }
        else if (Input.GetKey(KeyCode.E))
        {
            targetRigidbody.AddForce((targetHitPoint - baseTrans.position).normalized * pushForce, ForceMode.Acceleration);
            state = State.Pushing;
        }
    }

    void Update()
    {
        cursor.color = CursorColor;
        TargetDetection();

        if(state == State.Idle)
            telekinesisAudio.mute = true;
        else
            telekinesisAudio.mute = false;
    }

    void FixedUpdate()
    {
        PullingAndPushing();
    }
}
