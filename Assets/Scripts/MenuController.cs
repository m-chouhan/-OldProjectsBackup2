﻿using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuController : MonoBehaviour
{
    public static MenuController Instance;
    public AudioClip unselectAudioClip;

    private AudioSource audioSource;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }
    public GameObject startMenu;
    public GameObject waitingForPlayersMenu;
    public GameObject inGameMenu;
    public GameObject gameOverMenu;

    public TextMeshProUGUI score;

    public TextMeshPro oscore;
    public TextMeshProUGUI selection;
    public TextMeshProUGUI gameOverText;

    private List<Alphabet> currentSelection = new List<Alphabet>();
    private IDisposable clickSub;
    private Subject<Vector2> dragStream = new Subject<Vector2>();

    // Update is called once per frame
    void Update()
    {
        GameController.GameState state = GameController.Instance.GetState();
        switch(state) {
            case GameController.GameState.STARTED:
                score.SetText("SCORE " + GameController.Instance.SCORE);
                if(GameController.Instance.OSCORE > 0) {
                    oscore.SetText("OSCORE " + GameController.Instance.OSCORE);
                }
                break;
        }

        if(Input.touchCount > 0) {
            Debug.Log(Input.touchCount);
        }

        if(Input.touchCount == 2) {
            this.onSubmitClicked();
        }
    }

    internal void OnDrag(Vector2 position)
    {
        //Debug.Log("OnDrag[Menu]");
        dragStream.OnNext(position);
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        dragStream
        .Select(position => {
            var ray = Camera.main.ScreenPointToRay(position);
            RaycastHit hit = new RaycastHit();
            return (Physics.Raycast(ray, out hit)) ? hit.transform.gameObject : null;
        })
        .Where(gameObject => gameObject != null && gameObject.tag.Equals("Alphabet") && inGameMenu.activeSelf)
        //.ThrottleFirst(TimeSpan.FromMilliseconds(100))
        .DistinctUntilChanged(gameObject => {
            Alphabet alp = gameObject.GetComponent<Alphabet>();
            string output = alp.id.ToString() + alp.GetIsSelected();
            //Debug.Log(output);
            return output;
        })
        .Subscribe(OnSelectAlphabet);
    }

    internal void OnSelectAlphabet(GameObject item)    
    {
        if(!inGameMenu.activeSelf) {
            return ;
        }
        //Debug.Log("unirx : " + item.name);
        Alphabet alphabet = item.GetComponent<Alphabet>();
        int index = currentSelection.FindIndex(it => it.name.Equals(item.name));
        if (index != -1)
        {
            //Debug.Log("item already present, removing all proceeding indexes");
            for (int it = index + 1; it < currentSelection.Count; ++it)
            {
                currentSelection[it].SetIsSelected(false);
            }
            currentSelection
                .RemoveRange(index + 1, currentSelection.Count - index - 1);
        }
        else
        {
            currentSelection.Add(alphabet);
            alphabet.SetIsSelected(true);
        }
        string currentText = "";
        foreach (Alphabet alp in currentSelection)
            currentText += alp.character;
        selection.text = currentText;
    }

    public void EndGame(int score)
    {
        gameOverText
            .SetText("Score "+ score);
        gameOverMenu.SetActive(true);
        inGameMenu.SetActive(false);
        UnSelectAll();
        currentSelection = new List<Alphabet>();
        timer.Instance.reset();
        NetworkController.Instance.reset();
    }

    public void DisableWaitingForPlayersMenu() {
        waitingForPlayersMenu.SetActive(false);
        inGameMenu.SetActive(true);
    }

    public void onSubmitClicked(bool isDrag = false)
    {
        List<int> idlist = new List<int>();
        currentSelection.ForEach(alphabet => idlist.Add(alphabet.id));
        GameController.Instance.UpdateScore(selection.text, idlist, isDrag);
    }

    internal void DestroySelection()
    {
        selection.text = "";
        HashSet<Alphabet> finalList = new HashSet<Alphabet>();
        foreach (Alphabet alphabet in currentSelection)
        {
            finalList.Add(alphabet);
            if (alphabet.alphabetType == Alphabet.TYPE.BOMBERMAN)
                alphabet.FindNeighbours().ForEach(a => finalList.Add(a));
        }
        float i = 0;
        foreach(Alphabet alphabet in finalList) {
            alphabet.Explode(i);
            i += 0.05f;
        }
        currentSelection.Clear();
        Alphabet.AudioPitch = 0.8f;
    }

    public void UnSelectAll()
    {
        selection.text = "";
        for (int i = 0; i < currentSelection.Count; ++i)
        {
            currentSelection[i].SetIsSelected(false);
            StartCoroutine(currentSelection[i].PutInvalidColorForFeedback());
        }
        currentSelection.Clear();
        AudioSource.PlayClipAtPoint(unselectAudioClip, transform.position);
    }

    public void reset() {
        GameObject[] alphabets = GameObject.FindGameObjectsWithTag("Alphabet");
        Debug.Log(alphabets.Length);
        foreach(GameObject item in alphabets) {
            Destroy(item);
        }
        gameOverMenu.SetActive(false);
        inGameMenu.SetActive(true);
        GameController.Instance.StartGame((int)GameController.Instance.currentGameMode);
        if(GameController.Instance.currentGameMode == GameController.Mode.LOCAL) {
            NetworkController.Instance.initializeSinglePlayerGame();
        }
    }

    public void onBackPressed() {
        Debug.Log("in back pressed");
        GameObject[] alphabets = GameObject.FindGameObjectsWithTag("Alphabet");
        Debug.Log(alphabets.Length);
        foreach(GameObject item in alphabets) {
            Destroy(item);
        }
        inGameMenu.SetActive(false);
        gameOverMenu.SetActive(false);
        startMenu.SetActive(true);
    }
}
