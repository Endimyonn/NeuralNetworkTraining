using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/*
 * NetLearner is the connection between the neural network and the simulation.
 * This class feeds info into the network and takes its output as controls for the agent's behavior.
 * As provided, it just moves arbitrarily and awards fitness value based on how far along the +x axis it has moved.
 * Eventually, it will just learn to move to the right as fast as possible.
 * 
 * That's not useful, so you are to adapt/modify this code to your own challenge.
 */

public class NetLearner : MonoBehaviour
{
    public NeuralNetwork net = null;

    public float maxMovementForce = 5f;

    public bool finished = false;

    public Action<NetLearner> OnFinish;
    private float runTime = 0f;

    public Rigidbody rb;
    private Renderer rend;

    public float fitness = 0f;
    [SerializeField] private float maxRunTime = 20f;


    void Start()
    {
        if (net == null)
        {
            // Create the neural network

            // This takes an array that tells the number of neurons in each layer.
            // The input layer should match the number of different pieces of information you want to put in.
            // The output layer should match the number of different pieces of information you are getting out.

            // The number and size of hidden (middle) layers is not terribly important, but deeper and bigger networks are capable
            // of more complex problem-solving.  Note that even small networks like this can solve surprisingly complex problems!
            net = new NeuralNetwork(new int[] { 11, 6, 4 });
        }

        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();
    }

    public void SetRunTime(float newMaxRunTime)
    {
        maxRunTime = newMaxRunTime;
    }

    public void Restart(Vector3 pos)
    {
        transform.position = pos;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        fitness = 0f;
        runTime = 0f;
        finished = false;
    }

    public void Mark()
    {
        // Make this agent red to stand out
        Color c = rend.material.color;
        c.r = 1f;
        c.g /= 2;
        c.b /= 2;
        rend.material.color = c;
    }
    
    private void Update()
    {
        if (finished)
            return;

        runTime += Time.deltaTime;
        if(runTime > maxRunTime)
        {
            finished = true;

            // When the timer runs out, calculate a fitness value that is used to determine which agent performed the best.
            fitness += transform.position.x;  // FIXME: This is just saying that the further in the +x direction, the better...

            // Done agents can be darkened
            Color c = rend.material.color;
            c.r /= 2;
            c.g /= 2;
            c.b /= 2;
            rend.material.color = c;

            OnFinish(this);
        }
    }

    void FixedUpdate()
    {
        if (finished || net == null)
            return;

        //int mask = ~LayerMask.GetMask("IgnoreOthers");

        // TODO: Fill up input layer with information that the network needs to consider
        float[] input = new float[11];
        input[8] = rb.velocity.x;
        input[9] = rb.velocity.y;
        input[10] = rb.velocity.z;

        // Interpret the output as movement controls
        float[] output = net.FeedForward(input);
        rb.AddForce(new Vector3(maxMovementForce*(output[0] - output[2]), 0f, maxMovementForce*(output[1] - output[3])));
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (finished)
            return;

        // Collisions and other events could affect the fitness value of an AI agent.
        // Some ideas:
        // If you hit a wall, get penalized.  Eventually, agents will avoid walls.
        // If you get to the goal zone, get a bonus!  This will incentivise future generations to do the same.
    }
}
