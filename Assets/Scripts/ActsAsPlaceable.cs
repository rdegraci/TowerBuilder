﻿/*
 *
Copyright 2018 Rodney Degracia

MIT License:

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*
*/


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;


/*
 * Add this script to any GameObject that is to be pickable and placeable.
 * 
 * Note: This script should be used with GameObjects that do not have a Rigidbody, 
 * since this script only modifies the GameObject transform.position
 * 
 */
 
public class ActsAsPlaceable : MonoBehaviour
{

    public enum PlaceableState
    {
        READY,
        NOSELECTED,
        ELIGIBLE_FOR_SELECTION,
        NOT_ELIGIBLE_FOR_SELECTION,
        SELECTED
    }

    [SerializeField]
    public Material onHoverMaterial;

    [SerializeField]
    public Material onSelectedMaterial;

    private Material saveMaterial;
    private Ray controllerRay;
    private float clampDistance;
    protected PlaceableState placeableState;

    protected Ray GetControllerRay()
    {
        return controllerRay;
    }

    protected float GetClampDistance()
    {
        return clampDistance;
    }

    private void Awake()
    {
        clampDistance = 0.0F;
        placeableState = PlaceableState.READY;
    }

    void Start()
    {
        saveMaterial = this.gameObject.GetComponent<Renderer>().material;
    }

    // Update is called once per frame
    void Update()
    {

    }


    protected void OnEnable()
    {
        ActsAsCursor.OnCursorMove += OnCursorMove;
        ActsAsCursor.OnCursorHover += OnCursorHover;
        ActsAsCursor.OnCursorStopHover += OnCursorStopHover;

        ActsAsInputController.OnTriggerDown += OnTriggerDown;
        ActsAsInputController.OnTriggerUp += OnTriggerUp;
    }

    protected void OnDisable()
    {
        ActsAsInputController.OnTriggerDown -= OnTriggerDown;
        ActsAsInputController.OnTriggerUp -= OnTriggerUp;

        ActsAsCursor.OnCursorMove -= OnCursorMove;
        ActsAsCursor.OnCursorHover -= OnCursorHover;
        ActsAsCursor.OnCursorStopHover -= OnCursorStopHover;


    }









    /*
     * We use a statemachine, since events may occur asynchronously,
     * to help maintain state.
     * 
     */
    protected void ExecuteStateMachine(PlaceableState sm)
    {
        switch (sm)
        {
            case PlaceableState.NOSELECTED:
                {
                    if (placeableState != PlaceableState.SELECTED)
                    {
                        return;
                    }
                    clampDistance = 0.0F;   // reset the clamp, because the Gameobject is no longer selected
                    placeableState = PlaceableState.READY;

                    this.gameObject.GetComponent<Renderer>().material = saveMaterial;

                    break;
                }
            case PlaceableState.NOT_ELIGIBLE_FOR_SELECTION:
                {
                    if (placeableState != PlaceableState.ELIGIBLE_FOR_SELECTION)
                    {
                        return;
                    }
                    placeableState = PlaceableState.READY;
                    break;
                }
            case PlaceableState.ELIGIBLE_FOR_SELECTION:
                {
                    if (placeableState != PlaceableState.READY)
                    {
                        return;
                    }
                    placeableState = PlaceableState.ELIGIBLE_FOR_SELECTION;
                    break;
                }
            case PlaceableState.SELECTED:
                {
                    if (placeableState != PlaceableState.ELIGIBLE_FOR_SELECTION)
                    {
                        return;
                    }
                    placeableState = PlaceableState.SELECTED;

                    this.gameObject.GetComponent<Renderer>().material = onSelectedMaterial;

                    break;
                }
            case PlaceableState.READY:
                {
                    break;
                }
            default:
                break;
        }
    }

    public void OnCursorMove(Ray controllerRay, Transform cursorTransform, RaycastHit? raycast)
    {
        if (placeableState == PlaceableState.SELECTED)
        {








            ///
            /// Calculate the distance of the original controller ray, when the game object was
            /// first selected
            ///
            var heading = GetComponent<Renderer>().bounds.center - this.controllerRay.origin;
            var distance = heading.magnitude;

            // Clamp the distance so that the distance from the InputController to the GameObject
            // does not change while the GameObject is selected.
            if (Mathf.Abs(clampDistance - 0) < float.Epsilon)
            {
                clampDistance = distance;
            }

            // Move the game Object to a position on the Ray, at the clamped distance
            Vector3 position = controllerRay.GetPoint(clampDistance);
            this.transform.position = position;


        }
        if (placeableState == PlaceableState.NOSELECTED)
        {
            this.controllerRay = controllerRay;
        }

    }

    public void OnCursorHover(GameObject gameObject, Transform cursorTransform, RaycastHit raycastHit)
    {






        ///
        /// Return if we are not the gameObject that is being hovered by the Cursor
        ///
        if (this.gameObject.GetInstanceID() != gameObject.GetInstanceID())
        {
            return;
        }


        this.gameObject.GetComponent<Renderer>().material = onHoverMaterial;

        ExecuteStateMachine(PlaceableState.ELIGIBLE_FOR_SELECTION);

    }

    public void OnCursorStopHover(GameObject gameObject)
    {
        ExecuteStateMachine(PlaceableState.NOT_ELIGIBLE_FOR_SELECTION);

        this.gameObject.GetComponent<Renderer>().material = saveMaterial;
    }


    public void OnTriggerDown(byte controllerId, float value, GameObject gameObject, Transform cursorTransform)
    {
        ExecuteStateMachine(PlaceableState.SELECTED);
    }

    public void OnTriggerUp(byte controllerId, float value, GameObject gameObject, Transform cursorTransform)
    {
        ExecuteStateMachine(PlaceableState.NOSELECTED);
    }

}
