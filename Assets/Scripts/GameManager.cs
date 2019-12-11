using System.Collections;
using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public PlayableDirector smellSeq = null;
    private DogMovement dog = null; 
    bool playedSeq = false;
    

    // Start is called before the first frame update
    void Start()
    {
        dog = FindObjectOfType<DogMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlaySmellSeq() {
        if (!playedSeq) {
            smellSeq.Play();
            smellSeq.stopped += OnSmellSeqEnd;
        }
        playedSeq = true;
    }

    private void OnSmellSeqEnd(PlayableDirector smellSeq) {
        dog.OnTirePickedUp();
        // If you want something to happen after the sequence is done - this is the place to do it
    }

}
