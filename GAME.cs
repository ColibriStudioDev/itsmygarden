using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class GAME : MonoBehaviour
{
    public TMPro.TextMeshProUGUI dropInfoTtx;
    public TMPro.TextMeshProUGUI timertxt;
    public TMPro.TextMeshProUGUI endtxt;


    public AudioSource soundSource;
    public AudioSource musicSource;

    public Image IAProg;
    public Image PlayerProg;

    public GameObject InGameHUD;
    public GameObject FinishedBoard;
    public GameObject StartBoard;

    public GameProperty propertiesProfile;
    public static GameProperty properties;
    public static GameEvents baseEvent;


    public static Transform origin;


    public static GameState state = GameState.InfoScreen;

    public static MAPMANAGER manager;

    public static Player player;
    public static Player IA;

    public int timer;

    Cell selectedCell;

    void Start()
    {
        Time.timeScale = 1;
        timer = 0;
        baseEvent = new GameEvents();
        properties = propertiesProfile;
        origin = gameObject.transform;
        manager = new MAPMANAGER(properties.MapSize, gameObject.GetComponent<GAME>());
        player = new Player(true);
        IA = new Player(false);
        SoundManager.Initialize(musicSource,soundSource);

        SoundManager.Play("Base1", true);
        FinishedBoard.SetActive(false);

        state = GameState.InfoScreen;
        StartBoard.SetActive(true);

    }

    public void LauchIA(Cell playerCell)
    {
        IA.AILauch(playerCell);
    }

    //BUTTON
    public void SETGAMESPEED(float speed)
    {
        Time.timeScale = speed;
    }
    public void PLAY()
    {
        state = GameState.Ingame;
        StartBoard.SetActive(false);
    }
    public void RESTART()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void Update()
    {
        if (state ==GameState.Ingame)
        {

            if (player.hasntStart)
            {
                if (player.amout == 0) dropInfoTtx.text = $"Next drop in :  {player.dropFrequence - player.dropTime} s";
                else dropInfoTtx.text = $"Drop :  {player.cellType} , Amount : {player.amout} \n Next drop in :  {player.dropFrequence - player.dropTime} s";
            }
            if (Input.GetMouseButton(0))
            {
                if (player.hasntStart)
                {
                    player.PlaceCell(manager.MouseToCell(Input.mousePosition));
                }
                else
                {
                    Cell cell = manager.MouseToCell(Input.mousePosition);
                    if (cell == null) return;
                    manager.SpawnCell(cell.cellCoord, CellType.Flower, true);
                    LauchIA(cell);
                    baseEvent.AddListener(new UnityAction(Timer));
                    player.hasntStart = true;
                }
            }
        }

        RefreshProg();

    }

    void Timer()
    {

        timer++;
        timertxt.text = ConvertTime(timer);
        if(timer >= properties.gameDuration)
        {
            Time.timeScale = 0;
            state = GameState.GameFinished;
            timer = 0;
            GameFinished();
        }
    }


    void GameFinished()
    {
        if (player.IACells.Count < IA.IACells.Count) endtxt.text = "You lose, try again :=)";
        FinishedBoard.SetActive(true);
    }


    void RefreshProg()
    {

        if (player.hasntStart == false || IA.hasntStart == false) return;
        int iaCells = IA.IACells.Count;
        int playerCells = player.IACells.Count;

        int totalCells = iaCells + playerCells;
        

        PlayerProg.fillAmount = (float) playerCells / (float)totalCells;
        IAProg.fillAmount = (float) iaCells / (float)totalCells;

        if (iaCells == 0 || playerCells == 0) timer = (int)properties.gameDuration;
    }
    string ConvertTime(int timeleft)
    {
        int timing = (int)GAME.properties.gameDuration - timeleft;
        return string.Format("Time left : {0}min {1}sec", (int)timing / 60, timing % 60);
    }
}

public class MAPMANAGER
{

    public static Cell[,] map;
    public static Dictionary<int, Cell> findCell;
    public GAME instance;

    public MAPMANAGER(Vector2 size,GAME game)
    {
        int idToSet = 0;
        map = new Cell[(int)size.x, (int)size.y];
        findCell = new Dictionary<int, Cell>();

        instance = game;

        for(int y = 0; y < size.y; y++)
        {
            for(int x = 0; x < size.x; x++)
            {
                map[x, y] = new Cell(new Vector2(x,y),idToSet);
                findCell.Add(idToSet, map[x, y]);
                idToSet++;
            }
        }


        instance.StartCoroutine(CLOCK());
    }
    

    //TIME CLOCK
    private IEnumerator CLOCK()
    {
        yield return new WaitForSeconds(GAME.properties.refreshTime);
        GAME.baseEvent.Invoke();
        instance.StartCoroutine(CLOCK());
        yield break;
    }


    //GEAR
    public bool CheckMapBound(Vector2 coord)
    {
        if (coord.x > GAME.properties.MapSize.x - 1 || coord.x < 0) return false;
        if (coord.y > GAME.properties.MapSize.y - 1 || coord.y < 0) return false;
        return true;
    }
    public Cell MouseToCell(Vector2 mousePos)
    {

        Vector3 pos = GameObject.FindObjectOfType<Camera>().ScreenToWorldPoint(mousePos);
        Debug.DrawRay(pos, Vector3.forward, Color.red, 10, false);
        Physics.Raycast(pos, Vector3.forward, out RaycastHit hit, Mathf.Infinity);
        if (hit.collider == null) return null;


        return findCell[hit.collider.gameObject.GetComponent<ID>().get_ID];

    }
    public Cell CoordToCell(Vector2 coord)
    {
        if (coord.x > GAME.properties.MapSize.x-1 || coord.x < 0) return null;
        if (coord.y > GAME.properties.MapSize.y-1 || coord.y < 0) return null;
        return map[(int)coord.x, (int)coord.y];
    }
    public void SpawnCell(Vector2 coord,CellType type,bool isPlayer)
    {
        int id = GetCellID(map[(int)coord.x, (int)coord.y]);

        switch (type)
        {
            case CellType.Base:
                map[(int)coord.x, (int)coord.y] = new Cell(coord, id);
                break;
            case CellType.Flower:
                map[(int)coord.x, (int)coord.y] = new Flower(coord, id,isPlayer);
                break;
            case CellType.Fertilizer:
                map[(int)coord.x, (int)coord.y] = new Fertilizer(coord, id);
                break;
            case CellType.AttackBoost:
                map[(int)coord.x, (int)coord.y] = new AttackBoost(coord, id);
                break;
            case CellType.Wall:
                map[(int)coord.x, (int)coord.y] = new Wall(coord, id);
                break;
            case CellType.DefendBoost:
                map[(int)coord.x, (int)coord.y] = new Defender(coord, id);
                break;

        }
        findCell[id]=map[(int)coord.x, (int)coord.y];


    }
    public void SpawnFlower(Vector2 coord,bool isPlayer,Flower parents,Cell modifier)
    {
        int id = GetCellID(map[(int)coord.x, (int)coord.y]);

        map[(int)coord.x, (int)coord.y] = new Flower(coord, id, isPlayer, parents, modifier);
        findCell[id] = map[(int)coord.x, (int)coord.y];
    }
    public void SpawnCell(Vector2 coord)
    {
        int id = GetCellID(map[(int)coord.x, (int)coord.y]);
        map[(int)coord.x, (int)coord.y] = new Cell(coord, id);
        findCell[id] = map[(int)coord.x, (int)coord.y];
    }   

    public int GetCellID(Cell cell)
    {
        return cell.prefab.gameObject.GetComponent<ID>().get_ID;
    }

}


public static class SoundManager{

    public static AudioSource MusicSource;
    public static AudioSource SoundSource;


    public static void Initialize(AudioSource musicscr,AudioSource soundscr)
    {
        MusicSource = musicscr;
        SoundSource = soundscr;
    }

    public static void Play(string sound,bool isMusic)
    {
        if (isMusic)
        {
            foreach(Sound clip in GAME.properties.Musics)
            {
                if(sound == clip.name)
                {
                    MusicSource.clip = clip.clip;
                    MusicSource.Play();
                }
            }

        }
        else
        {
            foreach (Sound clip in GAME.properties.Sounds)
            {
                if (sound == clip.name)
                {
                    SoundSource.clip = clip.clip;
                    SoundSource.Play();
                }
            }
        }
    }
}


//PLAYER
public class Player
{
    bool isPlayer;
    public bool hasntStart = false;

    public int dropTime;
    public int dropFrequence;


    public int amout;
    public CellType cellType;

    UnityAction playerClock;

    public List<Cell> IACells = new List<Cell>();

    public Player(bool _isPlayer)
    {

        dropFrequence = GAME.properties.dropFrequence;
        isPlayer = _isPlayer;
        playerClock += CHECKDROP;
        if (isPlayer == false) playerClock += AIACTION;
        GAME.baseEvent.AddListener(playerClock);
    }


    void CHECKDROP()
    {
        if (hasntStart == false) return;
        dropTime++;
        if (dropTime >= dropFrequence)
        {
            DROP();
            dropTime = 0;
        }
    }

    void DROP()
    {
        if (isPlayer) SoundManager.Play("Drop", false);
        cellType = (CellType) Random.Range((int)CellType.Fertilizer,(int) CellType.DefendBoost + 1);

        amout = (int)GAME.properties.dropAmountRange.y;
        amout += (int)Random.Range(GAME.properties.dropAmountRange.x, GAME.properties.dropAmountRange.z);
    }

    void AIACTION()
    {
        bool hasplaced = false ;

        Cell[] _IACells = IACells.ToArray();

        if(amout > 0)
        {

            foreach(Cell cell in _IACells)
            {
                Cell[] cellsAround = cell.GetCellsAround(cell.cellCoord);
                foreach(Cell testcell in cellsAround)
                {
                    if (testcell == null) return;
                    if(testcell.type == CellType.Base)
                    {
                        Debug.Log("base");
                        if (cellType != CellType.Wall && cellType != CellType.DefendBoost)
                        {
                            PlaceCell(testcell);
                            hasplaced = true;
                        }
                        else
                        {
                            int x = Random.Range((int)-1, (int)1);
                            int y = Random.Range((int)-1, (int)1);
                            Vector2 dir = new Vector2(x, y);

                            for(int i = 0;i < amout;i++)
                            {
                                dir *= i;
                                PlaceCell(GAME.manager.CoordToCell(testcell.cellCoord + dir));
                            }
                        }
                        break;
                    }
                }

                if(hasplaced == true) break;
            }

        }

      


    }

    public void AILauch(Cell playerCell)
    {
        hasntStart = true;
        Vector2 playerStart = playerCell.cellCoord;
        Vector2 testCoord = Vector2.zero;

        do
        {
            int x = Random.Range(0, (int)GAME.properties.MapSize.x);
            int y = Random.Range(0, (int)GAME.properties.MapSize.y);
            testCoord = new Vector2(x, y);
        } while (Vector2.Distance(playerStart, testCoord) < GAME.properties.maxDistance);

        GAME.manager.SpawnCell(testCoord, CellType.Flower, isPlayer);
    }

    public void PlaceCell(Cell oldcell)
    {
        if (amout <= 0) return;
        if (oldcell == null) return;
        if (oldcell.isPlayer != isPlayer && oldcell.type != CellType.Base) return;

        int oldID = oldcell.prefab.GetComponent<ID>().get_ID;
        Vector2 coord = oldcell.cellCoord;

        GAME.manager.SpawnCell(coord, cellType, isPlayer);
        

        amout--;


    }
}

//CELLS
public class Cell
{
    public Color color = Color.white;
    public bool isPlayer = false;
    public GameObject prefab;
    public SpriteRenderer renderer;
    public Vector2 cellCoord;
    public CellType type = CellType.Base;
    public UnityAction cellAction;

    public Cell(Vector2 coord,int _ID)
    {
        type = CellType.Base;
        cellCoord = coord;
        prefab = GameObject.Instantiate(GAME.properties.CellPrefabs, GAME.origin);
        prefab.transform.localPosition = new Vector3(coord.x * GAME.properties.Cellgap, coord.y * GAME.properties.Cellgap, 0);

        renderer = prefab.GetComponent<SpriteRenderer>();

        renderer.color = SetRDColor(GAME.properties.CellColor);

        prefab.AddComponent(typeof(ID));
        prefab.GetComponent<ID>().get_ID = _ID;
    }


    //GEAR
    public Color InterpolateColor(float t,Color target)
    {
        Color newcolor = renderer.color;
        newcolor = Color.Lerp(newcolor, target,t);
        return newcolor;
    }
    public Color SetRDColor(Color baseColor)
    {
        Color newColor = baseColor;
        newColor.a = 1;
        float grayS = Random.Range(-GAME.properties.colorGap,GAME.properties.colorGap);
        newColor.b += grayS;
        newColor.g += grayS;
        newColor.r += grayS;

        return newColor;
    }
    public Vector2[] GetCoordAround(Vector2 coord)
    {
        Vector2[] temp = new Vector2[8];

        temp[0] = new Vector2(coord.x + 1, coord.y + 1);
        temp[1] = new Vector2(coord.x - 1, coord.y - 1);

        temp[2] = new Vector2(coord.x + 1, coord.y);
        temp[3] = new Vector2(coord.x, coord.y + 1);

        temp[4] = new Vector2(coord.x - 1, coord.y);
        temp[5] = new Vector2(coord.x, coord.y - 1);

        temp[6] = new Vector2(coord.x - 1, coord.y + 1);
        temp[7] = new Vector2(coord.x + 1, coord.y - 1);

        return temp;

    }
    public Cell[] GetCellsAround(Vector2 coord)
    {
        Cell[] temp = new Cell[8];
        Vector2[] aroundCoord = GetCoordAround(coord);

        for(int i = 0;i<temp.Length;i++)
        {
            temp[i] = GAME.manager.CoordToCell(aroundCoord[i]);           
        }

        return temp;
    }

    ~Cell()
    {
        Object.Destroy(prefab.gameObject);
    }
}
public class Flower : Cell
{
    //PROPERTY
    int maxGrow;
    int growForce;
    int maxLife;
    bool hasattacked;

    //STAT
    int grow;
    int life;
    int attack;
    int defend;




    public Flower(Vector2 coord, int _ID,bool _isPlayer) : base(coord, _ID)
    {
        if (_isPlayer) color = GAME.properties.FlowerColor;
        else color = GAME.properties.EnnemyFlowerColor;

        //SET RANDOM PROPERTY
        type = CellType.Flower;
        isPlayer = _isPlayer;
        GameProperty temp = GAME.properties;


        maxGrow = (int)temp.maxGrowRange.y;
        maxGrow += (int) Random.Range(temp.maxGrowRange.x, temp.maxGrowRange.z);

        growForce = (int)temp.growForceRange.y;
        growForce += (int)Random.Range(temp.growForceRange.x, temp.growForceRange.z);

        life = (int)temp.lifeRange.y;
        life += (int)Random.Range(temp.lifeRange.x, temp.lifeRange.z);

        attack = (int)temp.attackRange.y;
        attack += (int)Random.Range(temp.attackRange.x, temp.attackRange.z);

        defend = (int)temp.defendRange.y;
        defend += (int)Random.Range(temp.defendRange.x, temp.defendRange.z);

        maxLife = life;

        renderer.color = SetRDColor(color);
        cellAction += clockEvent;
        GAME.baseEvent.AddListener(cellAction);

        if (isPlayer == false) GAME.IA.IACells.Add(this);
        else GAME.player.IACells.Add(this); 
        
    }
    public Flower(Vector2 coord, int _ID, bool _isPlayer,Flower parents,Cell modifier) : base(coord, _ID)
    {
        //SET BASE PROPERTY

        if (_isPlayer) color = GAME.properties.FlowerColor;
        else color = GAME.properties.EnnemyFlowerColor;
        type = CellType.Flower;
        isPlayer = _isPlayer;
        renderer.color = SetRDColor(color);


        //SET RANDOM PROPERTY
        Vector3 _growRange=Vector3.zero;
        Vector3 _maxgrowRange = Vector3.zero;
        Vector3 _lifeRange = Vector3.zero;
        Vector3 _attackRange = Vector3.zero;
        Vector3 _defendRange = Vector3.zero;

        GameProperty property = GAME.properties;

        _growRange.x = property.growForceRange.x;
        _growRange.z = property.growForceRange.z;

        _maxgrowRange.x = property.maxGrowRange.x;
        _maxgrowRange.z = property.maxGrowRange.z;

        _lifeRange.x = property.lifeRange.x;
        _lifeRange.z = property.lifeRange.z;

        _attackRange.x = property.attackRange.x;
        _attackRange.z = property.attackRange.z;

        _defendRange.x = property.defendRange.x;
        _defendRange.z = property.defendRange.z;

        //Inheritance
        Flower flower = parents;
        _growRange.y = flower.growForce;
        _lifeRange.y = flower.maxLife;
        _attackRange.y = flower.attack;
        _defendRange.y = flower.defend;
        _maxgrowRange.y = flower.maxGrow;






        maxGrow = (int)_maxgrowRange.y;
        maxGrow += (int)Random.Range(_maxgrowRange.x, _maxgrowRange.z);

        growForce = (int)_growRange.y;
        growForce += (int)Random.Range(_growRange.x, _growRange.z);

        life = (int)_lifeRange.y;
        life += (int)Random.Range(_lifeRange.x, _lifeRange.z);

        attack = (int)_attackRange.y;
        attack += (int)Random.Range(_attackRange.x, _attackRange.z);

        defend = (int)_defendRange.y;
        defend += (int)Random.Range(_defendRange.x, _defendRange.z);

        maxLife = life;


        //CLAMP


        //APPLY MODIFIER
        if (modifier != null)
        {
            switch (modifier.type)
            {
                case CellType.Fertilizer:
                    Fertilizer fertilizer = (Fertilizer)modifier;
                    growForce *= (int)fertilizer.multiplier;
                    life -= fertilizer.lifeLess;
                    attack -= fertilizer.attackLess;
                    defend -= fertilizer.defendLess;
                    break;
                case CellType.AttackBoost:
                    AttackBoost attackBoost = (AttackBoost)modifier;
                    attack += attackBoost.attackBoost;
                    break;
                case CellType.DefendBoost:
                    Defender defender = (Defender)modifier;
                    defend += defender.defendBoost;
                    break;

            }
        }

        //CLAMP
        growForce = (int)Mathf.Clamp(growForce,2, GAME.properties.maxGrowForce);
        maxGrow = (int)Mathf.Clamp(maxGrow, GAME.properties.minMaxGrow, 300);

        life = (int)Mathf.Clamp(life, GAME.properties.minLife, 300);
        attack = (int)Mathf.Clamp(attack, GAME.properties.minAttack, 300);
        defend = (int)Mathf.Clamp(attack, GAME.properties.minDefend, 300);
        

        if (modifier != null)
        {
            if (modifier.type == CellType.DefendBoost) growForce = 0;
            if (modifier.type == CellType.AttackBoost)
            {
                growForce = 1;
                maxGrow = 15;
            }
        }
        //ADD INTERNAL LIFE CLOCK
        cellAction += clockEvent;
        GAME.baseEvent.AddListener(cellAction);

        if (isPlayer == false) GAME.IA.IACells.Add(this);
        else GAME.player.IACells.Add(this);


    }

    public void clockEvent()
    {
        Growth();
        AnalyseAround();
    }

    //ACTION
    public void Death()
    {
        if (isPlayer == false) GAME.IA.IACells.Remove(this);
        else GAME.player.IACells.Remove(this);
        GAME.baseEvent.RemoveListener(cellAction);
        GAME.manager.SpawnFlower(cellCoord,!isPlayer,this, null);
    }
    public void Growth()
    {
        grow += growForce;
        renderer.color = InterpolateColor((float)growForce / maxGrow,Color.white);

        if (grow >= maxGrow)
        {
            renderer.color = SetRDColor(color);
            GiveBirth();
            grow = 0;
        }

    }
    public void AnalyseAround()
    {
        hasattacked = false;
        int o = 0;
        Cell[] around = GetCellsAround(cellCoord);
        foreach(Cell cell in around)
        {
            if (cell == null) return;
            if (cell.type == CellType.Base) o++;
            else
            {
                SortInfo(cell);
            }
        }
        if (o == 8) return;
        
    }
    public void SortInfo(Cell cell)
    {
        switch (cell.type)
        {
            case CellType.Flower:
                if (cell.isPlayer != isPlayer && hasattacked == false)
                {
                    AttackFlower((Flower)cell);
                }
                break;
        }
    }
    public void AttackFlower(Flower cell)
    {
        cell.ReceiveAttack(attack);
        hasattacked = true;
    }
    public void GiveBirth()
    {
        Vector2[] checkCells = GetCoordAround(cellCoord);
        List<int> check = new List<int> {0,1,2,3,4,5,6,7};
        bool hasfound = false;

        for(int i = 0; i < checkCells.Length; i++)
        {
            int rdIndex = Random.Range(0, check.Count);
            int rd = check[rdIndex];
            if (checkCells[rd].x > GAME.properties.MapSize.x - 1 || checkCells[rd].x < 0) return;
            if (checkCells[rd].y > GAME.properties.MapSize.y -1 || checkCells[rd].y < 0) return;

            switch(MAPMANAGER.map[(int)checkCells[rd].x, (int)checkCells[rd].y].type)
            {
                case CellType.Base:
                    GAME.manager.SpawnFlower(checkCells[rd], isPlayer, this, null);
                    hasfound = true;
                    break;
                case CellType.Fertilizer:
                    GAME.manager.SpawnFlower(checkCells[rd], isPlayer, this, MAPMANAGER.map[(int)checkCells[rd].x, (int)checkCells[rd].y]);
                    hasfound = true;
                    break;
                case CellType.AttackBoost:
                    GAME.manager.SpawnFlower(checkCells[rd], isPlayer, this, MAPMANAGER.map[(int)checkCells[rd].x, (int)checkCells[rd].y]);
                    hasfound = true;
                    break;
                case CellType.DefendBoost:
                    GAME.manager.SpawnFlower(checkCells[rd], isPlayer, this, MAPMANAGER.map[(int)checkCells[rd].x, (int)checkCells[rd].y]);
                    hasfound = true;
                    break;

            }
            if (hasfound) break;
            check.RemoveAt(rdIndex);           
        }
        if (isPlayer) SoundManager.Play("Birth", false);

    }
    public void ReceiveAttack(int cellAttack)
    {
        int damage = cellAttack - defend;
        if (damage <= 0) damage = 0;
        life -= damage;

        if (life <= 0)
        {
            Death();
        }

        if (isPlayer) SoundManager.Play("Hit", false);

    }




}
public class Fertilizer : Cell
{
    //PROPERTY

    //STAT
    public float multiplier;
    public int defendLess;
    public int attackLess;
    public int lifeLess;



    public Fertilizer(Vector2 coord, int _ID) : base(coord, _ID)
    {
        color = GAME.properties.FertilizerColor;
        renderer.color = SetRDColor(color);
        type = CellType.Fertilizer;
        GameProperty temp = GAME.properties;

        //SET RANDOM PROPERTY
        multiplier = (int)temp.growRangeFactor.y;
        multiplier += (int)Random.Range(temp.growRangeFactor.x, temp.growRangeFactor.z);

        defendLess = (int)temp.defendLessRange.y;
        defendLess += (int)Random.Range(temp.defendLessRange.x, temp.defendLessRange.z);

        attackLess = (int)temp.attackLessRange.y;
        attackLess += (int)Random.Range(temp.attackLessRange.x, temp.attackLessRange.z);

        lifeLess = (int)temp.lifeLessRange.y;
        lifeLess += (int)Random.Range(temp.lifeLessRange.x, temp.lifeLessRange.z);

    }

}
public class AttackBoost : Cell
{
    //ATTACK BOOST
    public int attackBoost;


    public AttackBoost(Vector2 coord, int _ID) : base(coord, _ID)
    {
        type = CellType.AttackBoost;
        renderer.color = SetRDColor(GAME.properties.AttackBoostColor);

        attackBoost = (int)GAME.properties.AttackBoostRange.y;
        attackBoost = (int)Random.Range(GAME.properties.AttackBoostRange.x, GAME.properties.AttackBoostRange.z);

    }
}
public class Wall : Cell
{
    public Wall(Vector2 coord, int _ID) : base(coord, _ID)
    {
        type = CellType.Wall;
        renderer.color = SetRDColor(GAME.properties.WallColor);


    }
}
public class Defender : Cell
{
    public int defendBoost;


    public Defender(Vector2 coord, int _ID) : base(coord, _ID)
    {
        type = CellType.DefendBoost;
        renderer.color = SetRDColor(GAME.properties.DefendBoostColor);


        //SET RANDOM DEFEND BOOST

        defendBoost = (int)GAME.properties.DefendBoostRange.y;
        defendBoost += (int)Random.Range(GAME.properties.DefendBoostRange.x, GAME.properties.DefendBoostRange.y);

    }
}



//GEAR
class ID : MonoBehaviour
{
    private int Id = 0;

    public int get_ID { get => Id; set => Id = value; }
}
public class GameEvents : UnityEvent
{

}
[CreateAssetMenu(menuName =("Game/PropertiesProfile"))]
public class GameProperty : ScriptableObject
{
    [Header("General")]
    public Vector2 MapSize;
    public float colorGap = 0.1f;
    public float refreshTime = 1;

    public float gameDuration;

    public float maxGrowForce;
    public float minMaxGrow;

    public float maxDistance;

    [Header("DROP Properties")]
    public Vector3 dropAmountRange;
    public int dropFrequence;


    [Header("Basic Cell :")]
    public Color CellColor;
    public GameObject CellPrefabs;
    public float Cellgap = 0.32f;


    [Header("Flower Properties")]
    public Vector3 growForceRange;
    public Vector3 maxGrowRange;

    public Vector3 lifeRange;
    public Vector3 attackRange;
    public Vector3 defendRange;

    public float minLife;
    public float minAttack;
    public float minDefend;


    public Color FlowerColor;
    public Color EnnemyFlowerColor;

    [Header("Fertilizer Properties")]
    public Vector3 growRangeFactor;
    public Vector3 lifeLessRange;
    public Vector3 attackLessRange;
    public Vector3 defendLessRange;

    public Vector2 FertilizerSize;

    public Color FertilizerColor;

    [Header("Attack Boost")]
    public Vector3 AttackBoostRange;

    public Color AttackBoostColor;

    [Header("Wall")]
    public Color WallColor;

    [Header("Defend Boost")]
    public Vector3 DefendBoostRange;

    public Color DefendBoostColor;


    [Header("Sound")]
    public Sound[] Sounds;
    public Sound[] Musics;
}

[System.Serializable]
public class Sound
{
    public AudioClip clip;
    public string name;
}

public enum CellType
{
    Base,
    Flower,
    Fertilizer,
    AttackBoost,
    Wall,
    DefendBoost,
}

public enum GameState
{
    InfoScreen,
    Ingame,
    GameFinished,
}