﻿using System.Collections;
using System.Collections.Generic;
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
    THIncreseNum txtMeter;

    [SerializeField]
    CameraController cameraController;

    const int SCREEN_WIDTH = 1440;
    const int SCREEN_HEIGHT = 2560;

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

    int meter = 0;
    int breakCount = 0;
    int score = 0;

    int shouldBreak = 0;
    bool dieFlow = false;
    bool clearFlow = false;

    public void FeberTouch()
    {
        txtMeter.StartIncreseNum(meter += 100);
    }

    /// <summary>
    /// 큐빅을 터트렸을때
    /// </summary>
    /// <param name="shape"></param>
    public void AddBreakCount(bool isBreak)
    {
        if (!isBreak)
        {
            dieFlow = true;
            for (int i = 0; i < cubic.Length; i++)
            {
                cubic[i].RemoveAnim(false);
            }
            txtMeter.StartIncreseNum(0);
            player.SetState(Player.EState.Finish);
        }
        else
        {
            breakCount++;
            if (shouldBreak == breakCount)
            {
                clearFlow = true;
                for (int i = 0; i < cubic.Length; i++)
                {
                    cubic[i].RemoveAnim(false);
                }
                THHeightManager.Instance.AddHeight(THGameSetting.Instance.heightPerKillMob * breakCount);
                breakCount = 0;
                //txtMeter.StartIncreseNum(meter += 50);                
                player.SetState(Player.EState.Fly);
            }
        }
    }

    /// <summary>
    /// 인게임 게이지가 모두 소모할때
    /// </summary>
    public void EndGameTime()
    {
        if (dieFlow || clearFlow)
            return;

        dieFlow = true;
        for (int i = 0; i < cubic.Length; i++)
        {
            cubic[i].RemoveAnim(false);
        }
        THHeightManager.Instance.Drop();
        //txtMeter.StartIncreseNum(0);
        player.SetState(Player.EState.Finish);
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

        Invoke("testc",1.5f);
        THHPManager.Instance.Init();
    }

    private void testc()
    {
        cameraController.ShowFarAway();
        txtMeter.StartIncreseNum(meter += 300);
        player.SetState(Player.EState.Start);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetCubicRandomPosition();
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            for (int i = 0; i < cubic.Length; i++)
            {
                cubic[i].RemoveAnim(false);
            }
            player.SetState(Player.EState.Finish);
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            cameraController.ShowFarAway();
            txtMeter.StartIncreseNum(meter += 300);
            player.SetState(Player.EState.Start);
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
        clearFlow = false;
        _positionIndex.Clear();
        _numberIndex.Clear();
        shouldBreak = 0;

        for (int i = 0; i < cubic.Length; i++)
        {
            cubic[i].SetPosition(GetRandomPosition());
            bool type = Random.Range(0, 2) == 1;
            cubic[i].SetBreak(type);
            if (type)
            {
                shouldBreak++;
            }
            cubic[i].Appear();
        }
    }
}