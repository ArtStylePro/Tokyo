﻿using System;
using System.Collections.Generic;
using Assets.Scripts.Helpers;
using UnityEngine;

namespace Assets.Scripts.Arena
{
    public class Brain : MonoBehaviour
    {
        private readonly string[] _menuStrings = {"Creation", "Edit", "Erase", "PlayMode"};
        public BrainStates BrainState;
        public List<Deployable> DeployableList;
        public Grid GameGrid;
        public Transform GridTransform;
        public GUILocationHelper Location = new GUILocationHelper();
        private bool _allowToMove;
        private Deployable _currentObject;
        private Matrix4x4 _guiMatrix;
        private bool _isDown;
        private GridCell _lastVisitedTile;
        private int _menuSelectedIndex;
        private GridCell _originCell;
        private Deployable _selectedObject;
        private Vector3 _selectedObjectDeltaPosition;

        public void Start()
        {
            Location.PointLocation = GUILocationHelper.Point.BottomLeft;
            Location.UpdateLocation();


            Vector2 ratio = Location.GuiOffset;
            _guiMatrix = Matrix4x4.identity;
            _guiMatrix.SetTRS(new Vector3(1, 1, 1), Quaternion.identity, new Vector3(ratio.x, ratio.y, 1));

            BrainState = BrainStates.EditMode;

            if (!GridTransform)
            {
                Debug.LogWarning("Grid Transform is Missing!");
                GridTransform = GameObject.FindWithTag("Grid").transform;
            }
            if (!GameGrid)
            {
                Debug.LogWarning("Game Grid is Missing!");
                GameGrid = GameObject.FindWithTag("Grid").GetComponent<Grid>();
            }
        }

        public void OnGUI()
        {
            GUI.Label(new Rect(0, 0, 100, 50), BrainState.ToString());
            _menuSelectedIndex = GUI.Toolbar(new Rect(0, Location.Offset.y - 100, 300, 75), _menuSelectedIndex,
                _menuStrings);

            switch (BrainState)
            {
                case BrainStates.PlayMode:
                    break;
                case BrainStates.EraserMode:
                    break;
                case BrainStates.EditMode:
                    if (_selectedObject)
                    {
                        GUI.Label(new Rect(0, 50, 400, 50),
                            String.Format("Selected Object: {0}", _selectedObject.name));
                    }
                    break;
                case BrainStates.CreationMode:
                    GUI.matrix = _guiMatrix;


                    for (int i = 0; i < DeployableList.Count; i++)
                    {
                        if (GUI.RepeatButton(new Rect(i*150, 100, 145, 100), DeployableList[i].GetDisplayName()))
                        {
                            if (DeployableList[i].DeploymentMethod == DeploymentMethod.Drag)
                            {
                                _isDown = true;
                            }
                            else if (DeployableList[i].DeploymentMethod == DeploymentMethod.Brush)
                            {
                            }
                            _currentObject = DeployableList[i];
                        }
                    }

                    GUI.matrix = Matrix4x4.identity;
                    break;
            }

            UpdateBrainState();
        }

        private void UpdateBrainState()
        {
            switch (_menuSelectedIndex)
            {
                case 0:
                    BrainState = BrainStates.CreationMode;
                    break;
                case 1:
                    BrainState = BrainStates.EditMode;
                    break;
                case 2:
                    BrainState = BrainStates.EraserMode;
                    break;
                case 3:
                    BrainState = BrainStates.PlayMode;
                    break;
            }
        }

        public void Update()
        {
            switch (BrainState)
            {
                case BrainStates.PlayMode:
                    break;
                case BrainStates.EraserMode:
                    EraserUpdate();
                    break;
                case BrainStates.EditMode:
                    EditUpdate();
                    break;
                case BrainStates.CreationMode:
                    CreationUpdate();
                    //#if UNITY_IPHONE || UNITY_ANDROID
                    //            HandleTouchEvents();
                    //#elif !UNITY_FLASH
                    //              CreationUpdate();
                    //#endif
                    break;
            }
        }


        private void CreationUpdate()
        {
            if (_currentObject)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (_currentObject.DeploymentMethod == DeploymentMethod.Brush)
                    {
                        _isDown = true;
                    }
                }
                // this code doesn't check tile status (empty or full)ness
                // WTF
                if (Input.GetMouseButtonUp(0))
                {
                    _isDown = false;
                    if (_currentObject && _lastVisitedTile)
                    {
                        if (_currentObject.DeploymentMethod == DeploymentMethod.Drag)
                        {
                            var newCell =
                                (Deployable) Instantiate(_currentObject, _lastVisitedTile.gameObject.transform.position,
                                    Quaternion.identity);
                            newCell.transform.parent = GridTransform;
                            newCell.gameObject.layer = 9;
                            newCell.ParentGridCell = _lastVisitedTile;

                            _lastVisitedTile.InCellObject = newCell;
                            _lastVisitedTile.IsEmpty = false;
                        }
                    }
                    _lastVisitedTile = null;
                }

                if (_isDown)
                {
                    DragCheck();
                }
            }
        }

        private void DragCheck()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            Physics.Raycast(ray, out hitInfo, 100);
            if (hitInfo.collider)
            {
                var gCell = hitInfo.collider.GetComponent<GridCell>();
                if (gCell)
                {
                    if (gCell.IsEmpty)
                    {
                        if (GameGrid.IsPlaceableWithOffset(_currentObject.TileMap, gCell))
                        {
                            if (_currentObject)
                            {
                                switch (_currentObject.DeploymentMethod)
                                {
                                    case DeploymentMethod.Brush:
                                        Vector3 pos = gCell.gameObject.transform.position;

                                        Vector2 wOffset =
                                            _currentObject.TileMap.GetWorldTransformOffset(GameGrid.CellWidth);


                                        // TODO: Need an update for supporting TileOffset
                                        pos.x += wOffset.x;
                                        pos.y += wOffset.y;

                                        var newCell =
                                            (Deployable)
                                                Instantiate(_currentObject, pos, Quaternion.identity);

                                        newCell.transform.parent = GridTransform;
                                        newCell.gameObject.layer = 9;
                                        newCell.ParentGridCell = gCell;
                                        GameGrid.UpdateTilesStateWithOffset(newCell, gCell, CellState.Full);
                                        gCell.InCellObject = newCell;

                                        break;
                                    case DeploymentMethod.Drag:
                                        // Wait for End of Drag!
                                        _lastVisitedTile = gCell;
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }


        private void EditUpdate()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _isDown = true;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitInfo;
                Physics.Raycast(ray, out hitInfo, 100, 1 << 9);
                if (hitInfo.collider)
                {
                    _selectedObject = hitInfo.collider.gameObject.GetComponent<Deployable>();
                    _originCell = _selectedObject.ParentGridCell;
                    _allowToMove = true;

                    // I am Cheating! :-)
                    GameGrid.UpdateTilesStateWithOffset(_selectedObject, _originCell, CellState.Empty);

                    _selectedObjectDeltaPosition = _selectedObject.transform.position -
                                                   Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    //Ignoreing Z Index
                    _selectedObjectDeltaPosition.z = 0;

                }
                else
                {
                    _allowToMove = false;
                }
            }

            if (_allowToMove)
            {
                Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                pos.z = _selectedObject.transform.position.z;
                _selectedObject.transform.position = pos + _selectedObjectDeltaPosition;
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (_allowToMove)
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hitInfo;
                    Physics.Raycast(ray, out hitInfo, 100, 1 << 8);
                    if (hitInfo.collider)
                    {
                        var gCell = hitInfo.collider.GetComponent<GridCell>();
                        if (gCell)
                        {
                            if (gCell.IsEmpty)
                            {
                                if (GameGrid.IsPlaceableWithOffset(_selectedObject.TileMap, gCell))
                                {
                                    gCell.InCellObject = _selectedObject;
                                    Vector3 pos = gCell.gameObject.transform.position;

                                    
                                    Vector2 wOffset =
                                        _selectedObject.TileMap.GetWorldTransformOffset(GameGrid.CellWidth);

                                    // TODO: Need an update for supporting TileOffset
                                    pos.x += wOffset.x;
                                    pos.y += wOffset.y;

                                    _selectedObject.transform.position = pos;
                                    _selectedObject.ParentGridCell = gCell;


                                    GameGrid.UpdateTilesStateWithOffset(_selectedObject, _originCell, CellState.Empty);
                                    GameGrid.UpdateTilesStateWithOffset(_selectedObject, gCell, CellState.Full);
                                    _originCell = null;
                                }
                                else
                                {
                                    ResetSelectedObjectPosition();
                                }
                            }
                            else
                            {
                                ResetSelectedObjectPosition();
                            }
                        }
                        else
                        {
                            ResetSelectedObjectPosition();
                        }
                    }
                    else
                    {
                        ResetSelectedObjectPosition();
                    }
                }

                _isDown = false;
                _allowToMove = false;
            }
        }

        private void ResetSelectedObjectPosition()
        {
            Vector3 pos = _originCell.transform.position;

            pos.x += _selectedObject.TileMap.TileSize.X/2f*GameGrid.CellWidth -
                     GameGrid.CellWidth/2f;
            pos.y -= _selectedObject.TileMap.TileSize.Y/2f*GameGrid.CellWidth -
                     GameGrid.CellWidth/2f;

            _selectedObject.transform.position = pos;


            GameGrid.UpdateTilesStateWithOffset(_selectedObject, _selectedObject.ParentGridCell, CellState.Full);
        }


        private void EraserUpdate()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _isDown = true;
            }
            if (_isDown)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitInfo;
                Physics.Raycast(ray, out hitInfo, 100, 1 << 8);
                if (hitInfo.collider)
                {
                    var gCell = hitInfo.collider.GetComponent<GridCell>();
                    if (gCell)
                    {
                        if (!gCell.IsEmpty)
                        {
                            // Worst Way possible to handle deletion!
                            if (gCell.InCellObject)
                            {
                                //Hold reference to CellObject to Destroy it after clearing the Grid
                                Deployable toDelete = gCell.InCellObject;
                                GameGrid.UpdateTilesStateWithOffset(gCell.InCellObject, gCell.InCellObject.ParentGridCell,
                                    CellState.Empty);
                                Destroy(toDelete.gameObject);
                            }
                        }
                    }
                }
            }
            if (Input.GetMouseButtonUp(0))
            {
                _isDown = false;
            }
        }
    }
}