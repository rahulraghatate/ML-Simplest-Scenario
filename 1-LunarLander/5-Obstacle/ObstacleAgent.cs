﻿using MLAgents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleAgent : Agent
{
    public GameObject Marker;
    public GameObject Target;
    public GameObject Floor;
    public float StartingHeightMin = -1f;
    public float StartingHeightMax = 1f;
    public float FloorOffsetMin = 0.2f;
    public float FloorOffsetMax = 0.5f;
    public float UpForce = 20f;
    public float MinRewardDistance = 0.1f;
    public float MaxRewardDistance = 1f;
    public float OnMarkerPoints = 0.1f;
    public float OnCollisionPoints = -10.0f;
    public float CollisionVelocityThreshold = 5f;
    public bool Visualize = true;

    bool collisionHappened = false; // Did a collision happen?

    Rigidbody MarkerRigidBody;
    BehaviorParameters behaviorParams;

    private void Start()
    {
        MarkerRigidBody = Marker.GetComponent<Rigidbody>();
        behaviorParams = GetComponent<BehaviorParameters>();
    }

    // How to reinitialize when the game is reset. The Start() of an ML Agent
    public override void AgentReset()
    {
        if (MarkerRigidBody != null)
        {
            MarkerRigidBody.velocity = Vector3.zero;
            MarkerRigidBody.rotation = Quaternion.identity;
            MarkerRigidBody.angularVelocity = Vector3.zero;
        }

        Target.transform.position = transform.position;
        Floor.transform.position = Target.transform.position - new Vector3(0, Random.Range(FloorOffsetMin, FloorOffsetMax), 0);
        Marker.transform.position = Target.transform.position + new Vector3(0, Random.Range(StartingHeightMin, StartingHeightMax), 0);
        collisionHappened = false;

        if (Visualize)
        {
            for (var y = -2f; y < 2f; y += 0.05f)
            {
                var reward = CalculateReward(y, Target.transform.position.y);
                var c = Color.Lerp(Color.black, Color.white, reward * 5);
                Debug.DrawLine(new Vector3(Marker.transform.position.x, y, Marker.transform.position.z), new Vector3(Marker.transform.position.x, y + 0.05f, Marker.transform.position.z), c, 30);
            }
        }
    }

    // Tell the ML algorithm everything you can about the current state
    public override void CollectObservations()
    {
        AddVectorObs(Target.transform.position.y - Marker.transform.position.y); // distance to target
        AddVectorObs(MarkerRigidBody.velocity.y); // Current velocity
        AddVectorObs(Floor.transform.position.y - Marker.transform.position.y); // distance to floor
    }

    // What to do every step. The Update() of an ML Agent
    public override void AgentAction(float[] actions)
    {
        // This example only uses continuous space
        if (behaviorParams.brainParameters.vectorActionSpaceType != SpaceType.Continuous)
        {
            Debug.LogError("Must be continuous state type");
            return;
        }

        float action_y = actions[0]; // The agent has only one possible action. Up/Down amount
        action_y = Mathf.Clamp(action_y, -1, 1); // Bound the action input from -1 to 1
        action_y = action_y * UpForce; // Scale in put to marker speed
        if (MarkerRigidBody != null)
        {
            MarkerRigidBody.AddForce(0, action_y, 0);
        }

        // There was a collision! End the game with a penalty.
        if (collisionHappened)
        {
            SetReward(OnCollisionPoints);
            Done();
        }
        else // Everything is fine, just set a reward
        {
            SetReward(CalculateReward(Marker.transform.position.y, Target.transform.position.y));
        }
        
    }

    public float CalculateReward(float markerY, float targetY)
    {
        var distance = Mathf.Clamp(Mathf.Abs(markerY - targetY), MinRewardDistance, MaxRewardDistance); // clamp from 0.1 to 1 so the math works

        return OnMarkerPoints / ((10f * distance) * (10f * distance));
    }

    public void CollisionHappened()
    {
        collisionHappened = true;
    }
}