using UnityEngine;
using System.Collections;

namespace Vuforia
{
    public class CylinderTargetTrackableEventHandler : MonoBehaviour, ITrackableEventHandler
    {
        #region PRIVATE_MEMBER_VARIABLES

        private TrackableBehaviour mTrackableBehaviour;

        #endregion // PRIVATE_MEMBER_VARIABLES

        # region PUBLIC_MEMBER_VARIABLES

        public static bool isTracked = false;
        #endregion // PUBLIC_MEMBER_VARIABLES
        #region UNTIY_MONOBEHAVIOUR_METHODS

        void Start()
        {
            mTrackableBehaviour = GetComponent<TrackableBehaviour>();
            if (mTrackableBehaviour)
            {
                mTrackableBehaviour.RegisterTrackableEventHandler(this);
            }
        }

        void LateUpdate()
        {
            foreach(MeshRenderer r in GetComponents<MeshRenderer>())
            {

            }

        }
        #endregion // UNTIY_MONOBEHAVIOUR_METHODS



        #region PUBLIC_METHODS

        /// <summary>
        /// Implementation of the ITrackableEventHandler function called when the
        /// tracking state changes.
        /// </summary>
        public void OnTrackableStateChanged(
                                        TrackableBehaviour.Status previousStatus,
                                        TrackableBehaviour.Status newStatus)
        {
            if (newStatus == TrackableBehaviour.Status.DETECTED ||
                newStatus == TrackableBehaviour.Status.TRACKED ||
                newStatus == TrackableBehaviour.Status.EXTENDED_TRACKED)
            {
                OnTrackingFound();
            }
            else
            {
                OnTrackingLost();
            }
        }

        #endregion // PUBLIC_METHODS



        #region PRIVATE_METHODS


        private void OnTrackingFound()
        {
            isTracked = true;
            Renderer[] rendererComponents = GetComponentsInChildren<Renderer>(true);
            Collider[] colliderComponents = GetComponentsInChildren<Collider>(true);

            // Enable rendering:
            foreach (Renderer component in rendererComponents)
            {

                if (component.gameObject.tag.Equals("TacArm"))
                {
                    component.enabled = !UserUpdate.HideTACArm;
                }
                else
                {
                    component.enabled = true;
                }
            }

            foreach(MeshRenderer r in GetComponents<MeshRenderer>())
            {
                r.enabled = false;
            }

            // Enable colliders:
            foreach (Collider component in colliderComponents)
            {

                if (component.gameObject.tag.Equals("TacArm"))
                {
                    component.enabled = !UserUpdate.HideTACArm;
                }
                else
                {
                    component.enabled = true;
                }
            }

            //Debug.Log("Trackable " + mTrackableBehaviour.TrackableName + " found");
        }


        private void OnTrackingLost()
        {
            isTracked = false;

            Renderer[] rendererComponents = GetComponentsInChildren<Renderer>(true);
            Collider[] colliderComponents = GetComponentsInChildren<Collider>(true);

            // Disable rendering:
            foreach (Renderer component in rendererComponents)
            {
                component.enabled = false;
            }

            // Disable colliders:
            foreach (Collider component in colliderComponents)
            {
                component.enabled = false;
            }

            //Debug.Log("Trackable " + mTrackableBehaviour.TrackableName + " lost");
        }

        #endregion // PRIVATE_METHODS
    }
}