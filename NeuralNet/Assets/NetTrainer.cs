using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crosstales.FB;

/*
 * NetTrainer is in charge of creating and managing NetLearners as they are trained.
 * It runs the trials and selects the winners to clone for the next trial.
 * Clones are mutated according to the mutation parameters to try to arrive at better behavior randomly.
 * 
 * This object is also responsible for saving and loading a serialized copy of the best neural network so far.
 * Warning: Saving saves the "best" agent according to its *current* fitness value.  (You could fix this...)
 */

public class NetTrainer : MonoBehaviour
{
    public GameObject learnerPrefab;
    public Transform startingPoint;

    public int numLearners = 10;
    public float promotionThreshold = 0.8f;  // percentile cutoff for best performers

    public bool automatic = false;  // Restart the trial as soon as it ends

    public List<NetLearner> learners = new List<NetLearner>();
    public List<NetLearner> finishedLearners = new List<NetLearner>();
    public float maxRunTime = 20f;

    public float mutationParameterChance = 10f;
    public float mutationMaxAmount = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        StartTraining();
    }

    public void StartTraining()
    {
        // Move all learners to starting point
        for (int i = 0; i < learners.Count; ++i)
        {
            learners[i].Restart(startingPoint.position);
            learners[i].Mark();
            learners[i].SetRunTime(maxRunTime);
        }

        int numSourceLearners = learners.Count;
        int sourceNetIndex = 0;

        // Fill up the rest of the list with new learners
        for (int i = learners.Count; i < numLearners; ++i)
        {
            GameObject go = Instantiate(learnerPrefab, startingPoint.position, Quaternion.identity);
            NetLearner lnr = go.GetComponent<NetLearner>();
            lnr.SetRunTime(maxRunTime);

            // If we have old learners, copy their neural networks and mutate them
            if (numSourceLearners > 0)
            {
                lnr.net = new NeuralNetwork(learners[sourceNetIndex].net);
                sourceNetIndex++;
                if (sourceNetIndex >= numSourceLearners)
                    sourceNetIndex = 0;

                lnr.net.Mutate(mutationParameterChance, mutationMaxAmount);
            }


            lnr.OnFinish += LearnerFinished;
            learners.Add(lnr);
        }
    }

    public void LearnerFinished(NetLearner lnr)
    {
        learners.Remove(lnr);
        finishedLearners.Add(lnr);

        if(automatic && learners.Count == 0)
        {
            EndRaceAndRestart();
        }
    }

    void EndRaceAndRestart()
    {
        if (saveAtEnd)
            DoSave();

        // Find the cutoff for the given percentile
        int index = Mathf.RoundToInt(finishedLearners.Count * promotionThreshold + 0.49f);

        finishedLearners.Sort((a, b) => { return (a.fitness < b.fitness ? -1 : a.fitness == b.fitness ? 0 : 1); });

        for (int i = 0; i < finishedLearners.Count; ++i)
        {
            if (i < index) // Destroy the failures
                Destroy(finishedLearners[i].gameObject);
            else  // Promote the winners
                learners.Add(finishedLearners[i]);
        }

        Debug.Log("Best finisher's fitness: " + finishedLearners[finishedLearners.Count-1].fitness);

        finishedLearners.Clear();

        FitnessTrigger[] fitnessTriggers = Object.FindObjectsOfType<FitnessTrigger>();
        foreach (FitnessTrigger fitnessTrigger in fitnessTriggers)
        {
            fitnessTrigger.alreadyTriggered.Clear();
        }
        Debug.Log("Reset " + fitnessTriggers.Length + " fitness trigger" + (fitnessTriggers.Length != 1 ? "s" : "") + "!");

        StartTraining();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            EndRaceAndRestart();
    }

    private string saveFile;
    private bool saveAtEnd = false;
    public void SaveAs()
    {
        if (finishedLearners.Count == 0)
        {
            Debug.Log("No learners have finished yet.  Please choose a file, then wait for this round to complete and the best learner will be saved.");
        }


        saveFile = FileBrowser.SaveFile("Save As...", Application.persistentDataPath, "SavedNeuralNet", "txt");
        saveAtEnd = true;
    }

    private void DoSave()
    {
        finishedLearners.Sort((a, b) => { return (a.fitness < b.fitness ? -1 : a.fitness == b.fitness ? 0 : 1); });
        NeuralNetwork net = finishedLearners[finishedLearners.Count - 1].net;

        net.Save(saveFile);
        saveAtEnd = false;

        Debug.Log("Saved NeuralNetwork to " + saveFile);
    }

    public void Load()
    {
        string filename = FileBrowser.OpenSingleFile("Load...", Application.persistentDataPath, "txt");

        NeuralNetwork net = NeuralNetwork.Load(filename);

        for (int i = 0; i < learners.Count; ++i)
            Destroy(learners[i].gameObject);
        learners.Clear();

        for (int i = 0; i < finishedLearners.Count; ++i)
            Destroy(finishedLearners[i].gameObject);
        finishedLearners.Clear();

        // Fill up the rest of the list with new learners
        for (int i = 0; i < numLearners; ++i)
        {
            GameObject go = Instantiate(learnerPrefab, startingPoint.position, Quaternion.identity);
            NetLearner lnr = go.GetComponent<NetLearner>();
            lnr.SetRunTime(maxRunTime);

            lnr.net = new NeuralNetwork(net);

            // Mutate once first?
            //lnr.net.Mutate(mutationParameterChance, mutationMaxAmount);


            lnr.OnFinish += LearnerFinished;
            learners.Add(lnr);
        }

        Debug.Log("Loaded NeuralNetwork.");
    }
}
