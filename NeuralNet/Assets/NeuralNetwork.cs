using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/*
 * This simple NeuralNetwork contains a sequence of layers.
 * Each layer contains a set of neurons.
 * Neurons have their own biases and they are connected to each other neuron in adjacent layers with a certain weight.
 * Neurons calculate their activation potential based on the following equation:
 * P = bias + weight1 * P1 + weight2 * P2 + weight3 * P3 + ...
 *  where P1, P2, P3, etc. are the activation potentials of neurons on the previous layer and
 *  weight1, weight2, weight3, etc. are the weights of the connections between this neuron and each of the neurons on the previous layer.
 *  
 *  The first layer is the input layer, where information is fed in from the simulation.
 *  The last layer is the output layer, which can be interpreted as the neural network's results/decisions.
 *  
 *  In a simulation, information that the neural network needs to know about its environment (e.g. wall distances, speed components, etc.)
 *  is put into the input layer.  There should be one input neuron for each piece of input data.  The resulting data from the output layer 
 *  is used to control the AI agent's behavior during the simulation (e.g. movement along an axis, whether or not to jump, etc.).
 *  
 *  If the weights and biases are adjusted or selected based on some criteria of success toward a goal, then the network "learns" to do the task better over many trials.
 *  There are good ways to improve the learning of the neural network (e.g. backpropagation), but it will still work with enough training.
 */

public class NeuralNetwork
{
    public class Neuron
    {
        public float potential;
        public float bias;

        public Neuron()
        {
            potential = 0f;
            bias = Random.Range(-0.5f, 0.5f);
        }

        public Neuron(Neuron copy)
        {
            potential = copy.potential;
            bias = copy.bias;
        }

        public void MutateBiases(float percentChance, float amount)
        {
            if (Random.Range(0f, 100f) <= percentChance)
                bias += Random.Range(-amount, amount);
        }
    }

    public class Layer
    {
        public List<Neuron> neurons = new List<Neuron>();

        public void FeedForward(Layer previousLayer, int thisIndex, WeightsHolder weights, System.Func<float, float> Activation)
        {
            // For each neuron in this layer...
            for (int j = 0; j < neurons.Count; ++j)
            {
                // Get values from all of the neurons in the previous layer
                float value = 0f;
                for (int k = 0; k < previousLayer.neurons.Count; ++k)
                {
                    // Sum up all of the weighted potentials from the previous neurons
                    value += weights.GetWeight(thisIndex - 1, k, j) * previousLayer.neurons[k].potential;
                }
                // Add the bias, pass it through a smoothing function, and that is the next neuron's potential.
                neurons[j].potential = Activation(value + neurons[j].bias);
            }
        }

        public void MutateBiases(float percentChance, float amount)
        {
            for(int i = 0; i < neurons.Count; ++i)
            {
                neurons[i].MutateBiases(percentChance, amount);
            }
        }
    }

    public struct Pair<T, U>
    {
        public T first;
        public U second;

        public Pair(T first, U second)
        {
            this.first = first;
            this.second = second;
        }
    }


    [System.Serializable]
    public class WeightsHolder
    {
        // Maps neuron index from layer A to neuron index from layer B
        // For performance reasons, leaving it as an unfriendly jagged array
        public float[][][] weights;

        public WeightsHolder(List<Layer> layers)
        {
            // Need weights between each pair of layers
            weights = new float[layers.Count - 1][][];
            for (int i = 1; i < layers.Count; ++i)
            {
                // Need an array for each neuron in the layer
                weights[i - 1] = new float[layers[i].neurons.Count][];
                int neuronsInPreviousLayer = layers[i - 1].neurons.Count;
                // Go through each neuron in the last layer, connect it to each neuron in the next layer with a weight.
                for (int j = 0; j < layers[i].neurons.Count; ++j)
                {
                    // Each neuron has a connection to every neuron in the next layer
                    weights[i - 1][j] = new float[neuronsInPreviousLayer];
                    for (int k = 0; k < neuronsInPreviousLayer; ++k)
                    {
                        // Initialize a random weight for this connection
                        weights[i - 1][j][k] = Random.Range(-0.5f, 0.5f);
                    }
                }
            }
        }

        public WeightsHolder()
        {
            weights = null;
        }

        public WeightsHolder(WeightsHolder copy)
        {
            if (copy.weights == null)
            {
                weights = null;
                return;
            }

            weights = new float[copy.weights.Length][][];
            for (int i = 0; i < copy.weights.Length; ++i)
            {
                int neuronsInCurrentLayer = copy.weights[i].Length;
                weights[i] = new float[neuronsInCurrentLayer][];
                for (int j = 0; j < neuronsInCurrentLayer; ++j)
                {
                    int neuronsInPreviousLayer = copy.weights[i][j].Length;
                    weights[i][j] = new float[neuronsInPreviousLayer];
                    for (int k = 0; k < neuronsInPreviousLayer; ++k)
                    {
                        weights[i][j][k] = copy.weights[i][j][k];
                    }
                }
            }
        }

        public float GetWeight(int layerA, int neuronA, int neuronB)
        {
            return weights[layerA][neuronB][neuronA];
        }

        public void Mutate(float percentChance, float amount)
        {
            for (int i = 0; i < weights.Length; ++i)
            {
                for (int j = 0; j < weights[i].Length; ++j)
                {
                    for (int k = 0; k < weights[i][j].Length; ++k)
                    {
                        if (Random.Range(0f, 100f) <= percentChance)
                            weights[i][j][k] += Random.Range(-amount, amount);
                    }
                }
            }
        }
    }

    public List<Layer> layers = new List<Layer>();
    public WeightsHolder weights;

    public NeuralNetwork()
    {
        
    }

    public NeuralNetwork(int[] layerSizes)
    {
        // For each layer...
        for(int i = 0; i < layerSizes.Length; ++i)
        {
            Layer layer = new Layer();
            // Set up all the neurons for this layer
            for (int n = 0; n < layerSizes[i]; ++n)
            {
                layer.neurons.Add(new Neuron());
            }
            layers.Add(layer);
        }

        weights = new WeightsHolder(layers);
    }


    public NeuralNetwork(NeuralNetwork copy)
    {
        layers.Clear();

        // For each layer...
        for (int i = 0; i < copy.layers.Count; ++i)
        {
            Layer layer = new Layer();
            // Set up all the neurons for this layer
            for (int n = 0; n < copy.layers[i].neurons.Count; ++n)
            {
                layer.neurons.Add(new Neuron(copy.layers[i].neurons[n]));
            }
            layers.Add(layer);
        }

        weights = new WeightsHolder(copy.weights);
    }

    public float Activation(float x)
    {
        return (float)System.Math.Tanh(x);
    }

    public float[] FeedForward(float[] inputs)
    {
        for(int i = 0; i < inputs.Length && i < layers[0].neurons.Count; ++i)
        {
            layers[0].neurons[i].potential = inputs[i];
        }

        for(int i = 1; i < layers.Count; ++i)
        {
            layers[i].FeedForward(layers[i-1], i, weights, Activation);
        }

        // Return the output layer (last layer)
        List<Neuron> lastLayerNeurons = layers[layers.Count - 1].neurons;

        float[] result = new float[lastLayerNeurons.Count];
        for(int i = 0; i < lastLayerNeurons.Count; ++i)
        {
            result[i] = lastLayerNeurons[i].potential;
        }
        return result;
    }

    public void Mutate(float percentChance, float amount)
    {
        for (int i = 0; i < layers.Count; ++i)
        {
            layers[i].MutateBiases(percentChance, amount);
        }

        weights.Mutate(percentChance, amount);

    }

    public static NeuralNetwork Load(string filepath)
    {
        StreamReader reader = new StreamReader(filepath);
        string json = reader.ReadToEnd();
        reader.Close();

        //JsonUtility.FromJsonOverwrite(json, this);
        return JsonConvert.DeserializeObject<NeuralNetwork>(json);
    }

    public void Save(string filepath)
    {
        //string json = JsonUtility.ToJson(this);
        string json = JsonConvert.SerializeObject(this);

        StreamWriter writer = new StreamWriter(filepath);
        writer.Write(json);
        writer.Close();
    }
}
