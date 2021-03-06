﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class GameEngine : MonoBehaviour
{
    [SerializeField]
    Camera camera;

    [SerializeField]
    Cubic[] cubic;

    [SerializeField]
    Player player;

    [SerializeField]
    FiberBar fiberBar;

    [SerializeField]
    Button fiberButton;

    [SerializeField]
    GameObject gameUI;

    [SerializeField]
    GameObject hpBarUI;

    [SerializeField]
    THIncreseNum txtMeter;

    [SerializeField]
    CanvasGroup fadeBox;

    [SerializeField]
    public HitBossPanel bossPanel;

    [SerializeField]
    CameraController cameraController;

    [SerializeField]
    AudioSource explode_bgm;

    [SerializeField]
    Animator explode;

    const int SCREEN_WIDTH = 1440;
    const int SCREEN_HEIGHT = 2560;

    private bool blockTouch = false;

    /// <summary>
    /// 스크린 크기에 맞게 화면 리사이즈
    /// </summary>
    public void SetResolution()
    {
        Screen.SetResolution(1440, 2560,true);
        if (Screen.width / Screen.height < SCREEN_WIDTH / SCREEN_HEIGHT)
        {
            float width = (SCREEN_HEIGHT * 0.5f) / SCREEN_HEIGHT * SCREEN_WIDTH;
            camera.orthographicSize = width / Screen.width * Screen.height;
        }
    }

    // -2.5 ~ 3 (55)
    // 0 ~ -5 (50)
    private Vector2 START_POSITION = new Vector2(-2.5f, 0);

    List<Vector2> _positionIndex    = new List<Vector2>();
    List<int> _numberIndex          = new List<int>();

    int shouldBreak = 0;
    int breakCount  = 0;
    int meter = 0;
    int score = 0;
    int stage = 0;

    bool startGame  = false;
    bool dieFlow    = false;
    bool clearFlow  = false;

    Vector2 effectPosition = Vector2.zero;

    public void FeberTouch()
    {
        THHeightManager.Instance.AddHeight(THGameSetting.Instance.heightPerKillMob * 2);
        //txtMeter.StartIncreseNum(meter += 100);
    }

    /// <summary>
    /// 큐빅을 터트렸을때
    /// </summary>
    /// <param name="shape"></param>
    public void AddBreakCount(EType type,GameObject go = null)
    {
        if (dieFlow)
            return;

        if (type == EType.NoneBreak)
        {
            explode_bgm.Play();
            THHPManager.Instance.ZeroHP();
            EndGame();
        }
        else if (type == EType.Break || type == EType.Boss)
        {
            // 보스는 최종 1개일때 파괴 가능하다
            if (type == EType.Boss && (shouldBreak - 1) > breakCount)
            {
                return;
            }
            if (type == EType.Boss && go != null)
            {
                go.GetComponent<Cubic>().RemoveAnim(true);
                return;                
            }

            breakCount++;

            explode.gameObject.SetActive(true);
            explode.transform.localPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            explode.Rebind();
            explode.Play("Explosion");

            explode_bgm.Play();

            if (shouldBreak == breakCount)
            {
                clearFlow = true;
                for (int i = 0; i < cubic.Length; i++)
                {
                    cubic[i].RemoveAnim(false);
                }
                int heightValue = THGameSetting.Instance.heightPerKillMob * breakCount;
                THHeightManager.Instance.AddHeight(heightValue);
                THSkyBackground.Instance.Scroll(1.0f, heightValue);
                breakCount = 0;
                //txtMeter.StartIncreseNum(meter += 50);                
                player.SetState(Player.EState.Fly);
            }
        }
        else if (type == EType.Fever)
        {
            if (fiberBar.AddFiberCount())
            {
                for (int i = 0; i < cubic.Length; i++)
                {
                    cubic[i].RemoveAnim(false);
                }
                clearFlow = true;
                breakCount = 0;
                player.SetState(Player.EState.Fly);
                fiberButton.gameObject.SetActive(true);
            }
        }
    }

    private void EndGame()
    {
        dieFlow = true;
        for (int i = 0; i < cubic.Length; i++)
        {
            cubic[i].RemoveAnim(false);
        }
        player.fiberBar.gameObject.SetActive(false);

        LeanTween.moveLocalY(player.characterRenderer.gameObject, -4.15f, 0.2f)
            .setOnComplete(() =>
            {
                player.animation.StartAnimation(player.characterRenderer, (int)Player.EState.Finish, 0.1f, () => {
                    LeanTween.moveLocalY(player.characterRenderer.gameObject, -14f, 0.5f).setOnComplete(() => {
                        THSkyBackground.Instance.StopBackground();
                        THCloudManager.Instance.AllClear();
                        player.controller.Show();
                        player.characterRenderer.transform.localPosition = new Vector3(0.15f, 2f, 0);

                        bossPanel.gameObject.SetActive(true);
                        LeanTween.alphaCanvas(bossPanel.GetComponent<CanvasGroup>(), 1, 0.5f)
                            .setOnComplete(() => {

                            });
                        bossPanel.StartUserBar();

                    });
                }, false);
            });
    }

    /// <summary>
    /// 인게임 게이지가 모두 소모할때
    /// </summary>
    public void EndGameTime()
    {
        if (dieFlow || clearFlow)
            return;

        EndGame();
    }

    private void Start()
    {
        fiberBar.SetFiberCallback(() => {
            fiberButton.gameObject.SetActive(false);
            player.SetState(Player.EState.Fly);
        });

        for (int i = 0; i < cubic.Length; i++)
        {
            cubic[i].transform.parent.gameObject.SetActive(false);
        }
        hpBarUI.gameObject.SetActive(true);
        THHPManager.Instance.Init();

        bossPanel.callback = () =>
        {
            player.cutAni.gameObject.SetActive(true);
            player.cutAni.StartAnimation(player.cutAni.GetComponent<SpriteRenderer>(), 0, 0.1f, () =>
            {
                player.cutAni.gameObject.SetActive(false);
                LeanTween.moveLocalY(player.characterRenderer.gameObject, -4.15f, 0.05f).setOnComplete(() => {
                    player.SetState(Player.EState.Finish);
                    hpBarUI.SetActive(false);
                });
            });
                
        };

        LeanTween.alphaCanvas(fadeBox, 0, 0.5f).setOnComplete(() => {
            fadeBox.gameObject.SetActive(false);
        });
    }

    public void GameEnd()
    {
        fadeBox.gameObject.SetActive(true);
        LeanTween.alphaCanvas(fadeBox, 1, 0.5f).setOnComplete(() =>
        {
            SceneManager.LoadScene("Dev_InGameLogic");
        });
    }

    private void Update()
    {
        if (THIntroManager.Instance.IsIntro() || blockTouch)
            return;

        if (Input.GetMouseButtonUp(0))
        {
            if (!startGame)
            {
                startGame = true;
                cameraController.ShowFarAway();
                THHeightManager.Instance.SetHeight(300);
                //txtMeter.StartIncreseNum(meter += 300);
                player.SetState(Player.EState.Start);

                LeanTween.moveLocalY(gameUI, 1086, 0.5f);
                LeanTween.alphaCanvas(gameUI.GetComponent<CanvasGroup>(), 1, 1.0f);
            }
            else if (bossPanel.gameObject.activeSelf)
            {
                blockTouch = true;
                bossPanel.ShowLevel(bossPanel.CheckTime());
            }
        }
        for (int k = 0;k < Input.touchCount;k++)
        {
            Touch touch = Input.GetTouch(k);
            Collider2D collision = Physics2D.OverlapPoint((Camera.main.ScreenToWorldPoint(touch.position)));

            if (collision) // Check if there is a collision, if collision != null
            {
                for (int i = 0; i < cubic.Length; i++)
                {
                    if (collision == cubic[i].GetComponent<Collider2D>())
                    {
                        cubic[i].MouseActive();
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 공 배치의 실질적 위치를 가져온다.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    Vector2 GetRealPosition(int x, int y)
    {
        return new Vector2(-2.5f + (float)x / 10, 0 - (float)y / 10);
    }

    /// <summary>
    /// 겹치지 않는 포지션을 선정하기 위함
    /// </summary>
    /// <returns></returns>
    Vector2 GetRandomPosition()
    {
        Vector2 position = Vector2.zero;
        int splitX = 4;
        int splitY = 4;
        int x = Random.Range(0, 55 / splitX) * splitX;
        int y = Random.Range(0, 50 / splitY) * splitY;
        position = GetRealPosition(x,y);
        if (!_positionIndex.Contains(position))
        {
            // 3 * 3 자리에는 위치할 수 없게 제작한다.
            _positionIndex.Add(GetRealPosition(x - splitX, y - splitY));
            _positionIndex.Add(GetRealPosition(x - splitX, y));
            _positionIndex.Add(GetRealPosition(x - splitX, y + splitY));

            _positionIndex.Add(GetRealPosition(x + splitX, y - splitY));
            _positionIndex.Add(GetRealPosition(x + splitX, y));
            _positionIndex.Add(GetRealPosition(x + splitX, y + splitY));

            _positionIndex.Add(GetRealPosition(x, y + splitY));
            _positionIndex.Add(GetRealPosition(x, y - splitY));

            _positionIndex.Add(position);
            return position;
        }
        else
        {
            return GetRandomPosition();
        }
    }

    /// <summary>
    /// 중복되지 않는 랜덤 숫자를 생성한다 (1 ~ 100)
    /// </summary>
    /// <returns></returns>
    int GetRandomNumber()
    {
        int random = Random.Range(1, 100);
        if (!_numberIndex.Contains(random))
        {
            _numberIndex.Add(random);
            return random;
        }
        else
        {
            return GetRandomNumber();
        }
    }

    /// <summary>
    /// 큐빅을 랜덤으로 위치 시킨다.
    /// </summary>
    public void SetCubicRandomPosition()
    {
        shouldBreak = 0;
        clearFlow = false;
        _positionIndex.Clear();
        _numberIndex.Clear();

        THGameSetting.Level level = THGameSetting.Instance.gameLevel[THGameSetting.Instance.GetLevelPart(meter)];
        int monsterCount = Random.Range(level.minBreakCount, level.maxBreakCount + 1);
        int bombCount    = Random.Range(level.minNoneBreakCount, level.maxNoneBreakCount + 1);
        int feverCount   = (stage % 3 == 2) ? 1 :0;
        bool bossFade    = (Random.Range(0, 100) > 30);

        int allcount = monsterCount + bombCount + feverCount;
        for (int i = 0; i < allcount; i++)
        {
            cubic[i].SetPosition(GetRandomPosition());
            if (monsterCount > 0)
            {
                if (bossFade)
                {
                    cubic[i].SetType(EType.Boss);
                    bossFade = false;
                }
                else
                {
                    cubic[i].SetType(EType.Break);
                }
                shouldBreak++;
                monsterCount--;
            }
            else if (bombCount > 0)
            {
                cubic[i].SetType(EType.NoneBreak);
                bombCount--;
            }
            else if (feverCount > 0)
            {
                cubic[i].SetType(EType.Fever);
                feverCount--;
            }
            cubic[i].Appear();
        }
        stage++;
    }
}