using KModkit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class ColoredTileWithDependancies
{
	public int Index;
	public List<int> Dependancies;

    public ColoredTileWithDependancies()
    {
        Index = -1;
        Dependancies = new List<int>();
    }

    public ColoredTileWithDependancies(int index, List<int> dependancies)
	{
		Index = index;
		Dependancies = dependancies;
	}
}

// okay.
// i know what you're thinking.
// "why is this script called streetSlidingScript?"
// and that's because this module's name was street sliding,
// until i found something better,
// but i couldn't be bothered to change it.
// so just don't say anything,
// and we'll be good.
public class streetSlidingScript : MonoBehaviour {

    public KMBombModule module;
    public KMBombInfo info;
    public KMAudio bombAudio;
    static int ModuleIdCounter = 1;
    int ModuleId;
	bool moduleSolved = false;

    public KMSelectable[] trackSelectables;
	public Mesh[] tracksMeshes; //meshes are sorted numerically, with directions of the track corresponding to bits. up=1, down=2, left=4, right=8
	public MeshFilter[] trackFilters;
	public MeshRenderer[] trackRenderers; //my variable naming is soooo inconsistent lmfao

	public Material[] coloredMaterials;

	string[,] board = new string[4, 3]
	{
		{ "", "", "" },
		{ "", "", "" },
		{ "", "", "" },
		{ "", "", "" }
	};
    string[,] originalBoard = new string[4, 3]
    {
        { "", "", "" },
        { "", "", "" },
        { "", "", "" },
        { "", "", "" }
    };
    List<Vector2Int> unfilledSpaces = new List<Vector2Int>();

	int[,] tileSelectableIndices = new int[4, 3]
	{
		{ 0, 1, 2 },
		{ 3, 4, 5 },
		{ 6, 7, 8 },
		{ 9, 10, 11 }
	};
	Vector2Int initialHolePosition;
	Vector2Int holePosition;
	int holeIndex = -1;

	//colored tiles are sorted in the order: red, yellow, green, blue, pink
    Vector2Int[] coloredTileInitialPositions = new Vector2Int[5] { new Vector2Int(-1, -1), new Vector2Int(-1, -1), new Vector2Int(-1, -1), new Vector2Int(-1, -1), new Vector2Int(-1, -1) };
    Vector2Int[] coloredTilePositions = new Vector2Int[5] { new Vector2Int(-1, -1), new Vector2Int(-1, -1), new Vector2Int(-1, -1), new Vector2Int(-1, -1), new Vector2Int(-1, -1) }; //defining default values outside of the grid (0,0 is inside the grid)
	List<int> coloredTilesFindOrder = new List<int>();
    List<int>[] coloredTilesInRow = new List<int>[3] { new List<int>(), new List<int>(), new List<int>() };
	List<int> revealedColoredTiles = new List<int>();

	public KMSelectable querySelectable;
	public Material[] queryColorMaterials;
	public MeshRenderer[] queryScreenRenderers;
	bool tilesSlidable = true;
	bool canQuery = true;
	int maxQueryStage = 0;
	List<Vector2Int> allLoweredTiles = new List<Vector2Int>();

	Vector2Int[][][] queryTable = new Vector2Int[4][][]
	{
		new Vector2Int[15][] //
		{ 
			new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 1), new Vector2Int(5, 3), new Vector2Int(6, 4), new Vector2Int(7, 2) }, //up
			new Vector2Int[] { new Vector2Int(0, 6), new Vector2Int(2, 6), new Vector2Int(5, 0), new Vector2Int(6, 1), new Vector2Int(7, 5) }, //down
			new Vector2Int[] { new Vector2Int(0, 2), new Vector2Int(1, 4), new Vector2Int(1, 6), new Vector2Int(3, 1) }, //ud
			new Vector2Int[] { new Vector2Int(0, 7), new Vector2Int(2, 1), new Vector2Int(4, 0), new Vector2Int(5, 6), new Vector2Int(7, 1) }, //left
			new Vector2Int[] { new Vector2Int(0, 3), new Vector2Int(3, 4), new Vector2Int(3, 7), new Vector2Int(4, 3) }, //ul
			new Vector2Int[] { new Vector2Int(0, 5), new Vector2Int(3, 2), new Vector2Int(5, 5), new Vector2Int(6, 6) }, //dl
			new Vector2Int[] { new Vector2Int(1, 3), new Vector2Int(4, 5), new Vector2Int(5, 1), new Vector2Int(5, 4) }, //udl
			new Vector2Int[] { new Vector2Int(2, 2), new Vector2Int(2, 7), new Vector2Int(7, 0), new Vector2Int(7, 3) }, //right
			new Vector2Int[] { new Vector2Int(0, 4), new Vector2Int(2, 0), new Vector2Int(4, 7), new Vector2Int(6, 5) }, //ur
			new Vector2Int[] { new Vector2Int(2, 3), new Vector2Int(2, 5), new Vector2Int(5, 7), new Vector2Int(6, 0) }, //dr
			new Vector2Int[] { new Vector2Int(1, 5), new Vector2Int(1, 7), new Vector2Int(3, 0), new Vector2Int(4, 4), new Vector2Int(5, 2) }, //udr
			new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(2, 4), new Vector2Int(3, 3), new Vector2Int(3, 6) }, //lr
			new Vector2Int[] { new Vector2Int(3, 5), new Vector2Int(4, 1), new Vector2Int(6, 7), new Vector2Int(7, 4) }, //ulr
			new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(4, 6), new Vector2Int(6, 2), new Vector2Int(7, 6) }, //dlr
			new Vector2Int[] { new Vector2Int(1, 2), new Vector2Int(4, 2), new Vector2Int(6, 3), new Vector2Int(7, 7) }, //udlr
		},
        new Vector2Int[15][] //
		{
            new Vector2Int[] { new Vector2Int(1, 4), new Vector2Int(1, 7), new Vector2Int(2, 4), new Vector2Int(3, 2) }, //up
			new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(4, 2), new Vector2Int(4, 3), new Vector2Int(5, 7) }, //down
			new Vector2Int[] { new Vector2Int(2, 3), new Vector2Int(5, 0), new Vector2Int(5, 5), new Vector2Int(6, 5) }, //ud
			new Vector2Int[] { new Vector2Int(3, 7), new Vector2Int(5, 2), new Vector2Int(6, 6), new Vector2Int(7, 2) }, //left
			new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(1, 6), new Vector2Int(5, 4), new Vector2Int(7, 1), new Vector2Int(7, 5) }, //ul
			new Vector2Int[] { new Vector2Int(3, 4), new Vector2Int(4, 6), new Vector2Int(5, 1), new Vector2Int(7, 7) }, //dl
			new Vector2Int[] { new Vector2Int(2, 0), new Vector2Int(2, 6), new Vector2Int(3, 5) }, //udl
			new Vector2Int[] { new Vector2Int(2, 1), new Vector2Int(4, 0), new Vector2Int(6, 7), new Vector2Int(7, 6) }, //right
			new Vector2Int[] { new Vector2Int(0, 3), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(5, 6), new Vector2Int(6, 0), new Vector2Int(6, 2) }, //ur
			new Vector2Int[] { new Vector2Int(0, 2), new Vector2Int(0, 5), new Vector2Int(3, 3), new Vector2Int(6, 1) }, //dr
			new Vector2Int[] { new Vector2Int(1, 3), new Vector2Int(3, 6), new Vector2Int(4, 1), new Vector2Int(4, 5) }, //udr
			new Vector2Int[] { new Vector2Int(0, 7), new Vector2Int(1, 5), new Vector2Int(2, 7) }, //lr
			new Vector2Int[] { new Vector2Int(0, 4), new Vector2Int(4, 4), new Vector2Int(5, 3), new Vector2Int(7, 3), new Vector2Int(7, 4) }, //ulr
			new Vector2Int[] { new Vector2Int(2, 2), new Vector2Int(3, 0), new Vector2Int(4, 7), new Vector2Int(6, 4), new Vector2Int(7, 0) }, //dlr
			new Vector2Int[] { new Vector2Int(0, 6), new Vector2Int(2, 5), new Vector2Int(3, 1) }, //udlr
        },
        new Vector2Int[15][] //
		{
            new Vector2Int[] { new Vector2Int(0, 2), new Vector2Int(2, 0), new Vector2Int(3, 3), new Vector2Int(3, 6) }, //up
			new Vector2Int[] { new Vector2Int(0, 5), new Vector2Int(2, 4), new Vector2Int(2, 7), new Vector2Int(5, 5) }, //down
			new Vector2Int[] { new Vector2Int(1, 7), new Vector2Int(3, 5), new Vector2Int(4, 4), new Vector2Int(6, 1) }, //ud
			new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 4), new Vector2Int(2, 5), new Vector2Int(4, 3), new Vector2Int(5, 1) }, //left
			new Vector2Int[] { new Vector2Int(1, 1), new Vector2Int(5, 6), new Vector2Int(6, 0), new Vector2Int(6, 2), new Vector2Int(7, 4) }, //ul
			new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(4, 1), new Vector2Int(4, 5), new Vector2Int(6, 3) }, //dl
			new Vector2Int[] { new Vector2Int(1, 5), new Vector2Int(2, 2), new Vector2Int(3, 0), new Vector2Int(6, 3), new Vector2Int(7, 0), new Vector2Int(7, 3), new Vector2Int(7, 5) }, //udl
			new Vector2Int[] { new Vector2Int(0, 3), new Vector2Int(0, 7), new Vector2Int(1, 6), new Vector2Int(2, 3), new Vector2Int(7, 2) }, //right
			new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(5, 2), new Vector2Int(6, 4), new Vector2Int(6, 7) }, //ur
			new Vector2Int[] { new Vector2Int(1, 3), new Vector2Int(2, 6), new Vector2Int(3, 2), new Vector2Int(7, 6) }, //dr
			new Vector2Int[] { new Vector2Int(2, 1), new Vector2Int(3, 7), new Vector2Int(7, 1), new Vector2Int(7, 4), new Vector2Int(7, 7) }, //udr
			new Vector2Int[] { new Vector2Int(3, 1), new Vector2Int(5, 7) }, //lr
			new Vector2Int[] { new Vector2Int(0, 6), new Vector2Int(1, 4), new Vector2Int(3, 4), new Vector2Int(4, 6), new Vector2Int(6, 5) }, //ulr
			new Vector2Int[] { new Vector2Int(1, 2), new Vector2Int(4, 2), new Vector2Int(5, 0), new Vector2Int(5, 4) }, //dlr
			new Vector2Int[] { new Vector2Int(4, 0), new Vector2Int(4, 7), new Vector2Int(5, 3), new Vector2Int(6, 6) }, //udlr
        },
        new Vector2Int[15][] //
		{
            new Vector2Int[] { new Vector2Int(3, 0), new Vector2Int(4, 3), new Vector2Int(6, 0) }, //up
			new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(0, 3), new Vector2Int(1, 2), new Vector2Int(1, 5), new Vector2Int(2, 0), new Vector2Int(2, 2), new Vector2Int(3, 5), new Vector2Int(3, 7) }, //down
			new Vector2Int[] { new Vector2Int(0, 4), new Vector2Int(2, 6), new Vector2Int(6, 2) }, //ud
			new Vector2Int[] { new Vector2Int(0, 7), new Vector2Int(5, 3), new Vector2Int(5, 7), new Vector2Int(6, 5) }, //left
			new Vector2Int[] { new Vector2Int(3, 1), new Vector2Int(3, 3), new Vector2Int(6, 6), new Vector2Int(6, 7) }, //ul
			new Vector2Int[] { new Vector2Int(0, 6), new Vector2Int(4, 4), new Vector2Int(5, 0), new Vector2Int(7, 0) }, //dl
			new Vector2Int[] { new Vector2Int(0, 5), new Vector2Int(4, 1), new Vector2Int(6, 1), new Vector2Int(7, 6) }, //udl
			new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(3, 2), new Vector2Int(3, 6), new Vector2Int(6, 3), new Vector2Int(6, 4), new Vector2Int(7, 5) }, //right
			new Vector2Int[] { new Vector2Int(3, 4), new Vector2Int(4, 5), new Vector2Int(7, 1), new Vector2Int(7, 7) }, //ur
			new Vector2Int[] { new Vector2Int(2, 1), new Vector2Int(2, 7), new Vector2Int(4, 6), new Vector2Int(5, 2) }, //dr
			new Vector2Int[] { new Vector2Int(1, 6), new Vector2Int(2, 5), new Vector2Int(5, 1), new Vector2Int(5, 4) }, //udr
			new Vector2Int[] { new Vector2Int(4, 0), new Vector2Int(4, 2), new Vector2Int(7, 2), new Vector2Int(7, 3) }, //lr
			new Vector2Int[] { new Vector2Int(1, 1), new Vector2Int(1, 3), new Vector2Int(1, 7), new Vector2Int(4, 7) }, //ulr
			new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(1, 4), new Vector2Int(5, 5), new Vector2Int(5, 6) }, //dlr
			new Vector2Int[] { new Vector2Int(0, 2), new Vector2Int(2, 3), new Vector2Int(2, 4), new Vector2Int(7, 4) }, //udlr
        },
    };
	bool queryCycleOn = false;

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
		
		for(int i = 0; i < 12; i++)
		{
			int dummy = i;
			trackSelectables[dummy].OnInteract += delegate { TilePressed(dummy); return false; };
		}
		querySelectable.OnInteract += delegate { QueryPressed(); return false; };

		for(int i = 0; i < 4; i++)
		{
			int idummy = i;
			for(int j = 0; j < 3; j++)
			{
				int jdummy = j;
				unfilledSpaces.Add(new Vector2Int(idummy, jdummy)); //i just didnt want to type all this out
			}
		}
        unfilledSpaces.Remove(new Vector2Int(0, 2));
    }

    void Start() {
		GeneratePuzzle();

        //ShuffleTiles();
    }

	void TilePressed(int index)
	{
		if (!tilesSlidable || moduleSolved)
			return;

		//find position of tile pressed
		Vector2Int pressedPosition = new Vector2Int(-1, -1);
		for(int i = 0; i < 4; i++)
		{
            for (int j = 0; j < 3; j++)
            {
				if (tileSelectableIndices[i, j] == index)
				{
					int idummy = i;
					int jdummy = j;
					pressedPosition = new Vector2Int(idummy, jdummy);
					break;
				}
            }
			if (pressedPosition.x != -1)
				break;
        }

		//Debug.LogFormat("[Routing #{0}] Pressed {1}, {2}", ModuleId, pressedPosition.x, pressedPosition.y);

        Vector2Int pressedAndHoleDifference = new Vector2Int(Mathf.Abs(pressedPosition.x - holePosition.x), Mathf.Abs(pressedPosition.y - holePosition.y));
		if((pressedAndHoleDifference.x + pressedAndHoleDifference.y) <= 1) //manhattan distance of pressed tile is <= 1, which means the tile is adjacent to the hole
		{
            bombAudio.PlaySoundAtTransform("move" + Random.Range(1, 3), transform);
            StartCoroutine(SlideTile(trackSelectables[index].gameObject, holePosition));
            MoveTileToHole(pressedPosition);
        }	
	}

	void MoveTileToHole(Vector2Int tilePosition)
	{
		board[holePosition.x, holePosition.y] = board[tilePosition.x, tilePosition.y];
        tileSelectableIndices[holePosition.x, holePosition.y] = tileSelectableIndices[tilePosition.x, tilePosition.y];
		if(coloredTilePositions.Contains(tilePosition))
		{
			int index = System.Array.IndexOf(coloredTilePositions, tilePosition);
			coloredTilePositions[index] = holePosition;
		}

        holePosition = tilePosition;
        board[tilePosition.x, tilePosition.y] = "@";
		tileSelectableIndices[tilePosition.x, tilePosition.y] = holeIndex;
		//LogBoard();
    }

	void QueryPressed()
	{
		if (!canQuery || moduleSolved)
			return;

		if (tilesSlidable)
		{
			tilesSlidable = false;
			Debug.LogFormat("[Routing #{0}] Query pressed. State of board:", ModuleId);
			LogBoard();

			List<Vector2Int> fullTrack = AllConnectedTiles(board, new Vector2Int(0, 2));
			if(!fullTrack.Contains(new Vector2Int(3, 0)))
			{
                Debug.LogFormat("[Routing #{0}] The track does not connect from the bottom left to the top right. Strike!", ModuleId);
                module.HandleStrike();
				//return;
			}

			if(fullTrack.Count == 11 && EveryTrackConncted(fullTrack)) //has all tiles with no disconnects
			{
				module.HandlePass(); //honestly, if the defuser gets the solution early, just let them have it
				moduleSolved = true;
                queryScreenRenderers[0].material = queryColorMaterials[3];
                queryScreenRenderers[1].material = queryColorMaterials[3];
				bombAudio.PlaySoundAtTransform("solve", transform);
                return;
            }

			List<Vector2Int> removeThese = new List<Vector2Int>();
			foreach(var pos in fullTrack)
			{
				if(coloredTilePositions.Contains(pos))
				{
					int index = System.Array.IndexOf(coloredTilePositions, pos);
					print("tile " + index + " is not revealed");
					if (!revealedColoredTiles.Contains(index))
						removeThese.Add(pos);
				}
			}
			foreach (var pos in removeThese)
				fullTrack.Remove(pos);

			allLoweredTiles.Clear();
            StartCoroutine(LowerTiles(fullTrack));

			List<Vector2Int> cycle = DetermineQueryCycle(fullTrack);
			queryCycleOn = true;
			StartCoroutine(QueryCycle(cycle));
        }
		else
		{
			canQuery = false;
			queryCycleOn = false;
            StartCoroutine(LowerTiles(allLoweredTiles));
            StartCoroutine(RaiseTiles());
        }
    }

	List<Vector2Int> DetermineQueryCycle(List<Vector2Int> track)
	{
		//print("determining query cycle");
		List<int> coloredTilesToReveal = new List<int>();
        bool allRowTwo = true;
        bool allRowOne = true;

        foreach (var index in coloredTilesInRow[1])
        {
            if (!track.Contains(coloredTilePositions[index]))
            {
                allRowTwo = false;
                break;
            }
        }

        foreach (var index in coloredTilesInRow[0])
		{
			if (!track.Contains(coloredTilePositions[index]))
			{
				allRowOne = false;
				break;
			}
		}

		int tryQueryStage = maxQueryStage;
	TryLesserStage:
		switch (tryQueryStage)
		{
			case 2:
                if (!allRowOne || !allRowTwo) //likewise if stage check 1 fails go to stage 0 check
                {
                    tryQueryStage = 1;
                    goto TryLesserStage;
                }

                foreach (var index in coloredTilesInRow[2])
                {
                    coloredTilesToReveal.Add(index);
                    if (!revealedColoredTiles.Contains(index))
                        revealedColoredTiles.Add(index);
                }
                break;
            case 1: 
				if(!allRowOne) //likewise if stage check 1 fails go to stage 0 check
                {
					tryQueryStage = 0;
					goto TryLesserStage;
				}

                foreach (var index in coloredTilesInRow[1])
                {
                    coloredTilesToReveal.Add(index);
                    if (!revealedColoredTiles.Contains(index))
                        revealedColoredTiles.Add(index);
                }
                if (maxQueryStage < 2)
                    maxQueryStage = 2;
                break;
            case 0: 
					//this really only checks if the bottom left is connected to the bottom right.
				if (allRowTwo) //if we get to this point, both of them arent true and allRowOne isn't true. so, if allRowTwo is included but now allRowOne, we shouldnt show any query because thats not how the module works
					return new List<Vector2Int> { new Vector2Int(8, 8) };

				foreach (var index in coloredTilesInRow[0])
				{
					coloredTilesToReveal.Add(index);
					if (!revealedColoredTiles.Contains(index))
						revealedColoredTiles.Add(index);
				}
				if (maxQueryStage < 1)
					maxQueryStage = 1;
				break;
		}

		coloredTilesToReveal.OrderBy(x => coloredTilePositions[x].x);

		List<Vector2Int> queryColors = new List<Vector2Int>();
		for(int i = 0; i < coloredTilesToReveal.Count; i++)
		{
			//print("revealing " + coloredTilesToReveal[i]);
			Vector2Int pos = coloredTileInitialPositions[coloredTilesToReveal[i]];
            string coloredTile = originalBoard[pos.x, pos.y];
			queryColors.Add(queryTable[pos.x][(coloredTile.Contains("U") ? 1 : 0) +
					(coloredTile.Contains("D") ? 2 : 0) +
					(coloredTile.Contains("L") ? 4 : 0) +
					(coloredTile.Contains("R") ? 8 : 0) - 1].PickRandom());
		}
		queryColors.Add(new Vector2Int(8, 8));
		return queryColors;
	}

	bool TrackContainsAllTiles(List<Vector2Int> track, List<Vector2Int> tiles)
	{
		for (int i = 0; i < tiles.Count; i++)
		{
			if (!track.Contains(tiles[i]))
				return false;
		}
		return true;
	}

	void GeneratePuzzle()
	{
		//make basic track from bottom left to top right
        MakeBasicTrack();
		//decide hole position
        holePosition = unfilledSpaces.PickRandom();
        Debug.LogFormat("<Routing #{2}> Hole positioned at {0}, {1}", holePosition.x, holePosition.y, ModuleId);
        board[holePosition.x, holePosition.y] = "@"; //mark symbol as hole
        unfilledSpaces.Remove(holePosition);
		holeIndex = holePosition.x * 3 + holePosition.y;

        int coloredTileAmount = 0;
        for (int i = 0; i < 4; i++)
        {
            int idummy = i;
            for (int j = 0; j < 3; j++)
            {
                int jdummy = j;
                if (unfilledSpaces.Contains(new Vector2Int(i, j)))
                {
                    coloredTilePositions[coloredTileAmount] = new Vector2Int(idummy, jdummy);
                    coloredTileAmount++;
                    if (coloredTileAmount == 5)
                        break;
                }
            }
        }
		coloredTilePositions.Shuffle();
		//generate rest of the puzzle, rest of the tracks will be colored tiles
        PlaceRemainingTracks();

		//the colored tiles take up every space that isn't the default "bottom left to top right" track, so we already know the final states of every colored tile
		//the only part left is to figure out what row they should be placed in
		int[] rowOrder = new int[3];
		List<int> undeterminedRows = new List<int> { 0, 1, 2 };
		//find the first row that will be determined by query
		int indicatorDifference = info.GetOnIndicators().Count() - info.GetOffIndicators().Count(); //i dont know why these are methods this time
		if(indicatorDifference > 0) //positive, more lit
		{
			undeterminedRows.Remove(0);
			rowOrder[0] = 0;
		}
		else if(indicatorDifference < 0) //negative, more unlit
		{
            undeterminedRows.Remove(1);
            rowOrder[0] = 1;
        }
		else //zero
		{
            undeterminedRows.Remove(2);
            rowOrder[0] = 2;
        }
		//second row determined by query
		int serialNumber = info.GetSerialNumberNumbers().ToArray()[0];
		if(serialNumber % 2 == 0) //even
		{
            rowOrder[1] = undeterminedRows[0];
            rowOrder[2] = undeterminedRows[1];
        }
		else //odd
		{
            rowOrder[1] = undeterminedRows[1];
            rowOrder[2] = undeterminedRows[0];
        }
		string[] placementNames = new string[] { "first", "second", "third" };
		Debug.LogFormat("[Routing #{0}] The module's query will give you the tracks for the {1} row, the {2} row, then the {3} row.", ModuleId, placementNames[rowOrder[0]], placementNames[rowOrder[1]], placementNames[rowOrder[2]] );
		//find an order so that colored tiles can be found one at a time without making the next query unsolvable
		FindMinimalColoredTilePlacement();

		//log solution
        Debug.LogFormat("[Routing #{0}] Solution:", ModuleId);
        LogBoard();

        //decide how many colored tiles should be in each row
        int[] amountInRow = new int[3] { 1, 1, 1 };
        for (int i = 0; i < 2; i++)
            amountInRow[Random.Range(0, 3)]++;
		//shuffle board
		ShuffleBoard(amountInRow, rowOrder);
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 3; j++)
                originalBoard[i, j] = board[i, j];
        Debug.LogFormat("[Routing #{0}] Initial state:", ModuleId);
        LogBoard();

        SetSelectableMeshes();
    }

	void MakeBasicTrack()
	{
		List<char> unusedDirections = new List<char> { 'R', 'R', 'R', 'U', 'U' };
		ShuffleDirections:
		unusedDirections.Shuffle();
        //check the basic track to make sure every row has at least one empty space in it
        //this can be done by making sure the 3 R's and 2 U's aren't all in one group
        int groupLength = 1;
		for(int i = 1; i < 5; i++)
		{
			if (unusedDirections[i] == unusedDirections[i - 1])
			{
				groupLength++;
				if ((groupLength == 2 && unusedDirections[i] == 'U') || (groupLength == 3 && unusedDirections[i] == 'R'))
				{
					goto ShuffleDirections;
                }	
			}
			else
				groupLength = 1;
		}

		//we passed the test and can now put this on the board
		Vector2Int position = new Vector2Int(0, 2); //bottom left
		for(int i = 0; i < 5; i++)
		{
			if (unusedDirections[i] == 'U')
			{
				board[position.x, position.y] += "U";
                board[position.x, position.y - 1] += "D";
				position.y--;
            }
			else //R
			{
                board[position.x, position.y] += "R";
                board[position.x + 1, position.y] += "L";
                position.x++;
            }
            Vector2Int newPosition = new Vector2Int(position.x, position.y);
            unfilledSpaces.Remove(newPosition);
        }
	}

	void PlaceRemainingTracks()
	{
		Vector2Int[] offsets = new Vector2Int[4]
		{
			new Vector2Int(0, -1),
			new Vector2Int(0, 1),
			new Vector2Int(-1, 0),
			new Vector2Int(1, 0)
		};
		List<Vector2Int> branchTiles = new List<Vector2Int>();

		while(unfilledSpaces.Count > 0)
		{
			Vector2Int position = unfilledSpaces.PickRandom();
			branchTiles.Add(new Vector2Int(position.x, position.y));

			Debug.LogFormat("<Routing #{0}> Chose {1}, {2}", ModuleId, position.x, position.y);

			while(unfilledSpaces.Contains(position))
			{
				int index = Random.Range(0, 4); //randomly pick a direction
				Vector2Int chosenOffset = offsets[index];
				Vector2Int newPosition = position + chosenOffset;

                Debug.LogFormat("<Routing #{0}> Will walk in direction {1}", ModuleId, index);

				if (newPosition.x < 0 || newPosition.x > 3 || newPosition.y < 0 || newPosition.y > 2 || holePosition == newPosition) //if we would go out of bounds
				{
                    Debug.LogFormat("<Routing #{0}> Would walk out of bounds, retrying", ModuleId);
                    continue;
				}

				if(!branchTiles.Contains(newPosition)) //if this is already part of our branch then we shouldn't add a new connection as to not make any loops
				{ //if it is we'll still go there just no new connections will be made
                    Debug.LogFormat("<Routing #{0}> Walking into a tile not in branch", ModuleId);
                    branchTiles.Add(newPosition);
					switch(index)
					{
						case 0: //up
							board[position.x, position.y] += "U";
							board[newPosition.x, newPosition.y] += "D";
							break;
                        case 1: //down
                            board[position.x, position.y] += "D";
                            board[newPosition.x, newPosition.y] += "U";
                            break;
                        case 2: //left
                            board[position.x, position.y] += "L";
                            board[newPosition.x, newPosition.y] += "R";
                            break;
                        case 3: //right
                            board[position.x, position.y] += "R";
                            board[newPosition.x, newPosition.y] += "L";
                            break;
                    }
				}
				position = newPosition;
			}

            Debug.LogFormat("<Routing #{0}> Finished with this branch", ModuleId);
            foreach (var pos in branchTiles)
				unfilledSpaces.Remove(pos);
            branchTiles.Clear();
        }
	}

	void FindMinimalColoredTilePlacement()
	{
		List<int> independantColoredTiles = new List<int>();
		List<ColoredTileWithDependancies> dependantColoredTiles = new List<ColoredTileWithDependancies>();
		for(int i = 0; i < 5; i++)
		{
            int idummy = i;
            string tile = board[coloredTilePositions[i].x, coloredTilePositions[i].y];
			List<Vector2Int> coloredTileNeighbors = new List<Vector2Int>();

            foreach (char dir in tile)
			{
				switch(dir)
				{
					case 'U':
                        Vector2Int upNeighbor = new Vector2Int(coloredTilePositions[i].x, coloredTilePositions[i].y - 1);
                        if (!coloredTilePositions.Contains(upNeighbor) && holePosition != upNeighbor)
						{
							independantColoredTiles.Add(idummy);
						}
						else if(coloredTilePositions.Contains(upNeighbor))
						{
							coloredTileNeighbors.Add(upNeighbor);
						}
						break;
                    case 'D':
                        Vector2Int downNeighbor = new Vector2Int(coloredTilePositions[i].x, coloredTilePositions[i].y + 1);
                        if (!coloredTilePositions.Contains(downNeighbor) && holePosition != downNeighbor)
                        {
                            independantColoredTiles.Add(idummy);
                        }
                        else if (coloredTilePositions.Contains(downNeighbor))
                        {
                            coloredTileNeighbors.Add(downNeighbor);
                        }
                        break;
                    case 'L':
                        Vector2Int leftNeighbor = new Vector2Int(coloredTilePositions[i].x - 1, coloredTilePositions[i].y);
                        if (!coloredTilePositions.Contains(leftNeighbor) && holePosition != leftNeighbor)
                        {
                            independantColoredTiles.Add(idummy);
                        }
                        else if (coloredTilePositions.Contains(leftNeighbor))
                        {
                            coloredTileNeighbors.Add(leftNeighbor);
                        }
                        break;
                    case 'R':
                        Vector2Int rightNeighbor = new Vector2Int(coloredTilePositions[i].x + 1, coloredTilePositions[i].y);
                        if (!coloredTilePositions.Contains(rightNeighbor) && holePosition != rightNeighbor)
                        {
                            independantColoredTiles.Add(idummy);
                        }
                        else if (coloredTilePositions.Contains(rightNeighbor))
                        {
                            coloredTileNeighbors.Add(rightNeighbor);
                        }
                        break;
                }
            }
			if (independantColoredTiles.Contains(i))
				continue;

			//if we get to this point this tile is dependant on another tile being there
			//like, theoretically there could be more puzzle gen to determine if the tile can be by itself
			//but that's a whole can of worms i don't feel like getting into right now
			//maybe if i revisit this module the puzzle generation can be more sophisticated
			//but thats just secret santa 2 baby

			List<int> dependancyIndex = new List<int>();
			foreach (var pos in coloredTileNeighbors)
				dependancyIndex.Add(System.Array.IndexOf(coloredTilePositions, pos));
			dependantColoredTiles.Add(new ColoredTileWithDependancies(idummy, dependancyIndex));
		}

		List<int> undecidedColorTiles = new List<int> { 0, 1, 2, 3, 4 };
		while(coloredTilesFindOrder.Count < 5)
		{
			int colorTile = undecidedColorTiles.PickRandom();
			//print("Checking tile " + colorTile);
			if(independantColoredTiles.Contains(colorTile) || undecidedColorTiles.Count == 1)
			{
				//print("Connected to wood or is last tile to be revealed");
				coloredTilesFindOrder.Add(colorTile);
				undecidedColorTiles.Remove(colorTile);
                continue;
            }

			ColoredTileWithDependancies dependancies = new ColoredTileWithDependancies();
			for(int i = 0; i < dependantColoredTiles.Count; i++)
			{
				if (dependantColoredTiles[i].Index == colorTile)
				{
					dependancies = dependantColoredTiles[i];
					break;
                }
			}
			bool allTilesFoundBefore = false;
			foreach(var tile in dependancies.Dependancies)
			{
				//print("Checking if tile " + tile + " is placed");
				if (!undecidedColorTiles.Contains(tile))
					allTilesFoundBefore = true;
			}
			if (allTilesFoundBefore)
			{
				//print("One of the tiles this tile is dependant on is already placed");
				coloredTilesFindOrder.Add(colorTile);
                undecidedColorTiles.Remove(colorTile);
            }
        }
	}

	void ShuffleBoard(int[] amountInRow, int[] rowOrder)
	{
		int[,] originalBoard = new int[4, 3];
		for(int i = 0; i < 4; i++)
		{
			for(int j = 0; j < 3; j++)
			{
				originalBoard[i,j] = i * 3 + j;
			}
		}
		int[,] shuffledBoard = new int[4, 3] 
		{
			{ -1, -1, -1 },
			{ -1, -1, -1 },
			{ -1, -1, -1 },
			{ -1, -1, -1 },
		};
		List<int> remainingTiles = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

		int nextElement = 0;
		for(int i = 0; i < 3; i++) //place colored tiles
		{
			List<int> openSpotsInRow = new List<int> { 0, 1, 2, 3 };
			for(int j = 0; j < amountInRow[i]; j++)
			{
				int index = openSpotsInRow.PickRandom();
				openSpotsInRow.Remove(index);
				Vector2Int coloredTilePos = coloredTilePositions[coloredTilesFindOrder[nextElement]];
				//print("Colored tile position " + coloredTilePos.x + " " + coloredTilePos.y);
				shuffledBoard[index, rowOrder[i]] = originalBoard[coloredTilePos.x, coloredTilePos.y];

				int dummy = nextElement;
				int idummy = i;
                coloredTilesInRow[idummy].Add(coloredTilesFindOrder[dummy]);
				//print("added " + coloredTilesFindOrder[dummy] + " to row " + i);

				remainingTiles.Remove(originalBoard[coloredTilePos.x, coloredTilePos.y]);
                coloredTilePositions[coloredTilesFindOrder[nextElement]] = new Vector2Int(index, rowOrder[i]);
                coloredTileInitialPositions[coloredTilesFindOrder[nextElement]] = coloredTilePositions[coloredTilesFindOrder[nextElement]];
                nextElement++;
			}
		}

		List<Vector2Int> standardTilePositions = new List<Vector2Int>();
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 3; j++)
            {
				if (shuffledBoard[i, j] != -1)
					continue;

				int idummy = i;
				int jdummy = j;
				standardTilePositions.Add(new Vector2Int(idummy, jdummy));
				int tile = remainingTiles.PickRandom();
				remainingTiles.Remove(tile);
				shuffledBoard[i, j] = tile;
            }
        }

		while(!SolvablePuzzle(shuffledBoard)) //puzzle is unsolvable under our current metrics, try swapping two tiles until they do
		{
			//print("Swapping two tiles");
			Vector2Int firstRandomPosition = standardTilePositions.PickRandom();
			Vector2Int secondRandomPosition = standardTilePositions.PickRandom();
			if (firstRandomPosition == secondRandomPosition)
				continue;

			int firstTile = shuffledBoard[firstRandomPosition.x, firstRandomPosition.y];
			shuffledBoard[firstRandomPosition.x, firstRandomPosition.y] = shuffledBoard[secondRandomPosition.x, secondRandomPosition.y];
			shuffledBoard[secondRandomPosition.x, secondRandomPosition.y] = firstTile;
        }

        /*for (int i = 0; i < 3; i++)
        {
			string row = "";
            for (int j = 0; j < 4; j++)
            {
				row += shuffledBoard[j, i] + " ";
            }
			print(row);
        }*/

        SetBoard(shuffledBoard);
    }

	void SetBoard(int[,] setBoard)
	{
        string[,] originalBoard = new string[4, 3];
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 3; j++)
            {
				originalBoard[i, j] = board[i, j];
            }
        }
		for(int i = 0; i < 4; i++)
		{
			for(int j = 0; j < 3; j++)
			{
                Vector2Int pos = IndexToPosition(setBoard[i, j]);
                board[i, j] = originalBoard[pos.x, pos.y];

				if (board[i, j] == "@")
				{
					holePosition = new Vector2Int(i, j);
                    holeIndex = holePosition.x * 3 + holePosition.y;
                    initialHolePosition = holePosition;
                }
			}
		}
	}

	Vector2Int IndexToPosition(int index)
	{
		return new Vector2Int(Mathf.FloorToInt(index / 3), index % 3);
	}

	bool SolvablePuzzle(int[,] testBoard)
	{
		int[] tiles = new int[11];
		int nextTileIndex = 0;
		for (int i = 0; i < 4; i++)
			for (int j = 0; j < 3; j++)
			{
				if (testBoard[i, j] == holeIndex)
					continue;

				tiles[nextTileIndex] = testBoard[i, j];
				nextTileIndex++;
            }

		int swaps = 0;
		for(int i = 0; i < 10; i++)
		{
			int currentIndex = i + 1;
			while(currentIndex <= 10)
			{
				if (tiles[i] > tiles[currentIndex])
					swaps++;
				currentIndex++;
			}
		}
		return swaps % 2 == 0;
	}

    void SetSelectableMeshes()
	{
		for(int i = 0; i < 4; i++)
		{
			for(int j = 0; j < 3; j++)
			{
				if (coloredTilePositions.Contains(new Vector2Int(i, j)))
				{
                    trackRenderers[i * 3 + j].material = coloredMaterials[System.Array.IndexOf(coloredTilePositions, new Vector2Int(i, j))];
                    continue;
				}

				string boardSpace = board[i, j];

				if(boardSpace == "@") //make the tile where the hole should be unselectable
				{
					trackSelectables[tileSelectableIndices[i, j]].gameObject.SetActive(false);
					continue;
				}

				trackFilters[i*3 + j].mesh = tracksMeshes[
					(boardSpace.Contains("U") ? 1 : 0) +
					(boardSpace.Contains("D") ? 2 : 0) +
					(boardSpace.Contains("L") ? 4 : 0) +
					(boardSpace.Contains("R") ? 8 : 0)];
            }
		}
	}

	void ResetTiles()
	{
        float[] newXPosition = new float[4] { -0.05790111f, -0.01930035f, 0.0193004f, 0.05790114f };
        float[] newYPosition = new float[3] { 0.01930035f, -0.0193004f, -0.05790114f };
        for (int i = 0; i < 4; i++)
		{
			for(int j = 0; j < 3; j++)
			{
				Transform tile = trackSelectables[i * 3 + j].transform;
                tile.localPosition = new Vector3(newXPosition[i], tile.localPosition.y, newYPosition[j]);
				tileSelectableIndices[i, j] = i * 3 + j;
			}
		}

		holePosition = initialHolePosition;
		for (int i = 0; i < 5; i++)
			coloredTilePositions[i] = coloredTileInitialPositions[i];

        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 3; j++)
                board[i, j] = originalBoard[i, j];
    }

    IEnumerator SlideTile(GameObject tileObject, Vector2Int newPosition)
	{
		float[] newXPosition = new float[4] { -0.05790111f, -0.01930035f, 0.0193004f, 0.05790114f };
		float[] newYPosition = new float[3] { 0.01930035f, -0.0193004f, -0.05790114f };
		Vector3 newLocalPosition = new Vector3(newXPosition[newPosition.x], 0.01449489f, newYPosition[newPosition.y]);
		Vector3 oldLocalPosition = tileObject.transform.localPosition;

		//Debug.LogFormat("OLD POSITION {0} {1} {2}", oldLocalPosition.x, oldLocalPosition.y, oldLocalPosition.z);

		float t = 0;
		while(t < 1)
		{
			tileObject.transform.localPosition = Vector3.Lerp(oldLocalPosition, newLocalPosition, t);
			t += .25f;
			yield return new WaitForSeconds(.01f);
		}
		tileObject.transform.localPosition = newLocalPosition;

    }

	IEnumerator LowerTiles(List<Vector2Int> dontLower)
	{
		Vector2Int[][] lowerTilesInOrder = new Vector2Int[6][]
		{
			new Vector2Int[] { new Vector2Int(0,0) },
			new Vector2Int[] { new Vector2Int(0,1), new Vector2Int(1,0) },
			new Vector2Int[] { new Vector2Int(0,2), new Vector2Int(1,1), new Vector2Int(2,0) },
			new Vector2Int[] { new Vector2Int(1,2), new Vector2Int(2,1), new Vector2Int(3,0) },
			new Vector2Int[] { new Vector2Int(2,2), new Vector2Int(3,1) },
			new Vector2Int[] { new Vector2Int(3,2) },
		};
		for(int i = 0; i < 6; i++)
		{
			foreach(Vector2Int pos in lowerTilesInOrder[i])
			{
				if (!dontLower.Contains(pos))
				{
					allLoweredTiles.Add(pos);
					StartCoroutine(LowerTilesAnimation(trackSelectables[tileSelectableIndices[pos.x, pos.y]].gameObject));
				}
			}
			yield return new WaitForSeconds(.1f);
		}
		yield return null;
	}

    IEnumerator LowerTilesAnimation(GameObject tileObject)
    {
		float t = 0;
		while(t < 3)
		{
			tileObject.transform.localPosition = new Vector3(tileObject.transform.localPosition.x, 0.01449489f + (HeightAmount(t) * .025f), tileObject.transform.localPosition.z);
			t += .06f;
			yield return new WaitForSeconds(.01f);
		}
    }
	float HeightAmount(float t)
	{
		return -((t - 1) * (t - 1)) + 1;
	}

	IEnumerator RaiseTiles()
	{
		yield return new WaitForSeconds(1.6f);
        ResetTiles();

		List<Vector2Int> allPositions = new List<Vector2Int> {
			new Vector2Int(0, 0),
			new Vector2Int(0, 1),
			new Vector2Int(0, 2),
			new Vector2Int(1, 0),
			new Vector2Int(1, 1),
			new Vector2Int(1, 2),
			new Vector2Int(2, 0),
			new Vector2Int(2, 1),
			new Vector2Int(2, 2),
			new Vector2Int(3, 0),
			new Vector2Int(3, 1),
			new Vector2Int(3, 2)
		};

		while(allPositions.Count > 0)
		{
			Vector2Int pos = allPositions.PickRandom();
			allPositions.Remove(pos);
            GameObject tileObject = trackSelectables[tileSelectableIndices[pos.x, pos.y]].gameObject;
			StartCoroutine(RaiseTilesAnimation(tileObject));
            yield return new WaitForSeconds(.05f);
        }

		yield return new WaitForSeconds(.2f);
		tilesSlidable = true;
		canQuery = true;
    }

	IEnumerator RaiseTilesAnimation(GameObject tileObject)
	{
        float t = -1;
        while (t < 0)
        {
            tileObject.transform.localPosition = new Vector3(tileObject.transform.localPosition.x, 0.01449489f + (-t * t * .025f), tileObject.transform.localPosition.z);
            t += .05f;
            yield return new WaitForSeconds(.01f);
        }
    }

	IEnumerator QueryCycle(List<Vector2Int> colors)
	{
		int index = 0;
		while(queryCycleOn)
		{
			queryScreenRenderers[0].material = queryColorMaterials[colors[index].x];
			queryScreenRenderers[1].material = queryColorMaterials[colors[index].y];
			yield return new WaitForSeconds(1f);
			index++;
			index = index % colors.Count();
		}
		queryScreenRenderers[0].material = queryColorMaterials[8];
		queryScreenRenderers[1].material = queryColorMaterials[8];
	}

    void LogBoard()
	{
		char[] pathSymbols = new char[] { ' ', '╹', '╻', '┃', '╸', '┛', '┓', '┫', '╺', '┗', '┏', '┣', '━', '┻', '┳', '╋' };
		string[] coloredTileSymbols = new string[] { "R", "Y", "G", "B", "P" };

		for(int i = 0; i < 3; i++)
		{
			string row = "";
			for(int j = 0; j < 4; j++)
			{
				if(coloredTilePositions.Contains(new Vector2Int(j, i)))
				{
					int index = System.Array.IndexOf(coloredTilePositions, new Vector2Int(j, i));
					row += coloredTileSymbols[index];
					continue;
				}

				string tile = board[j, i];
				row += pathSymbols[(tile.Contains('U') ? 1 : 0) +
                    (tile.Contains('D') ? 2 : 0) +
                    (tile.Contains('L') ? 4 : 0) +
                    (tile.Contains('R') ? 8 : 0)];
			}
            Debug.LogFormat("[Routing #{0}] {1}", ModuleId, row);
        }
	}

	List<Vector2Int> AllConnectedTiles(string[,] checkBoard, Vector2Int position)
	{
		List<Vector2Int> connectedTilePositions = new List<Vector2Int> { position };
		Queue<Vector2Int> tileSearchQueue = new Queue<Vector2Int>();
		tileSearchQueue.Enqueue(position);

		string[] directions = new string[] { "U", "D", "L", "R" };
		string[] oppositeDirections = new string[] { "D", "U", "R", "L" };
		Vector2Int[] directionOffsets = new Vector2Int[] { new Vector2Int(0, -1), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(1, 0) };
		while(tileSearchQueue.Count > 0)
		{
			Vector2Int tilePosition = tileSearchQueue.Dequeue();
			string tile = checkBoard[tilePosition.x, tilePosition.y];

			for(int i = 0; i < 4; i++) //in all four directions
			{
				if (tile.Contains(directions[i])) //if the tile we're searching has a track going in this direction, it is attached to the tile above
				{
					int dummy = i;
					Vector2Int newTile = tilePosition + directionOffsets[dummy];
					if (connectedTilePositions.Contains(newTile) || //don't add a tile to be searched again if we already searched it
						newTile.x < 0 || newTile.x > 3 || newTile.y < 0 || newTile.y > 2 || //ignore out of bounds tiles
						!board[newTile.x, newTile.y].Contains(oppositeDirections[dummy])) 
						continue;

					connectedTilePositions.Add(newTile); //add the connected tile to the list of connected tiles
					tileSearchQueue.Enqueue(newTile); //add the connected tile to be searched for further connections
					//Debug.LogFormat("Tile {0}, {1} is connected to tile {2}, {3} by direction {4}", tilePosition.y, tilePosition.x, newTile.y, newTile.x, directions[i]);
				}
			}
        }

		return connectedTilePositions;
	}

    bool EveryTrackConncted(List<Vector2Int> track)
	{
        string[] directions = new string[] { "U", "D", "L", "R" };
        string[] oppositeDirections = new string[] { "D", "U", "R", "L" };
        Vector2Int[] directionOffsets = new Vector2Int[] { new Vector2Int(0, -1), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(1, 0) };

        foreach (var pos in track)
		{
			string tile = board[pos.x, pos.y];
            for (int i = 0; i < 4; i++) //in all four directions
            {
                if (tile.Contains(directions[i])) //if the tile we're searching has a track going in this direction, it is attached to the tile above
                {
                    int dummy = i;
                    Vector2Int newTile = pos + directionOffsets[dummy];
                    if (newTile.x < 0 || newTile.x > 3 || newTile.y < 0 || newTile.y > 2 || //ignore out of bounds tiles
                        !board[newTile.x, newTile.y].Contains(oppositeDirections[dummy]))
                        return false; //connection either goes out of bounds or the tile it connects to doesnt share the connection, making a disconnect
                }
            }
        }
		return true;
	}

    string[,] RemoveConnection(string[,] board, Vector2Int position, char direction)
	{
		board[position.x, position.y] = board[position.x, position.y].Replace(direction.ToString(), "");
        switch (direction)
        {
            case 'U':
                board[position.x, position.y - 1] = board[position.x, position.y - 1].Replace("D", "");
                break;
            case 'D':
                board[position.x, position.y + 1] = board[position.x, position.y + 1].Replace("U", "");
                break;
            case 'L':
                board[position.x - 1, position.y] = board[position.x - 1, position.y].Replace("R", "");
                break;
            case 'R':
                board[position.x + 1, position.y] = board[position.x + 1, position.y].Replace("L", "");
                break;
        }

		return board;
    }
}