using UnityEngine;
using System;
using System.Collections.Generic;

namespace DeepSky.Haze
{
    [Serializable, AddComponentMenu("")]
    public class DS_HazeContext
    {
        #region FIELDS
        [SerializeField]
        public List<DS_HazeContextItem> m_ContextItems;
        [SerializeField]
        private int m_SoloItem = -1;

        public int Solo { get { return m_SoloItem; } }
        #endregion

        /// <summary>
        /// Default constructor - with a 'Default' DS_HazeContextVariant and a time of 0.5 (midday).
        /// </summary>
        public DS_HazeContext()
        {
            m_ContextItems = new List<DS_HazeContextItem>();
            DS_HazeContextItem ctxItem = new DS_HazeContextItem();
            ctxItem.m_Name = "Default";
            m_ContextItems.Add(ctxItem);
        }

        /// <summary>
        /// Create and add a duplicate of the passed DS_HazeContextVariant.
        /// </summary>
        /// <param name="index"> Index into context variants list of the one to duplicate. </param>
        public void DuplicateContextItem(int index)
        {
            if (index < 0 || index >= m_ContextItems.Count) { return; }

            DS_HazeContextItem dup = new DS_HazeContextItem();
            dup.CopyFrom(m_ContextItems[index]);
            dup.m_Name += "_Copy";
            m_ContextItems.Add(dup);
        }

        /// <summary>
        /// Remove the context variant at index, as long as there is more than one context in
        /// the list.
        /// </summary>
        /// <param name="index"> The index of the context variant to remove. </param>
        public void RemoveContextItem(int index)
        {
            if (index < 0 || index >= m_ContextItems.Count || m_ContextItems.Count == 1) { return; }

            m_ContextItems.RemoveAt(index);

            // Update the solo index.
            if (m_SoloItem == -1) { return; }
            if (m_SoloItem == index) { m_SoloItem = -1; }
        }

        /// <summary>
        /// Move the context variant at index up in the list (unless already at the top).
        /// </summary>
        /// <param name="index"></param>
        public void MoveContextItemUp(int index)
        {
            if (index < 1 || index >= m_ContextItems.Count) { return; }

            DS_HazeContextItem tmp = m_ContextItems[index];
            m_ContextItems.RemoveAt(index);
            m_ContextItems.Insert(index - 1, tmp);

            // Update the solo index.
            if (m_SoloItem == -1) { return; }
            if (m_SoloItem == index) { m_SoloItem -= 1; }
            else if (m_SoloItem == index - 1) { m_SoloItem++; }
        }

        /// <summary>
        /// Move the context variant at index down in the list (unless already at the bottom).
        /// </summary>
        /// <param name="index"></param>
        public void MoveContextItemDown(int index)
        {
            if (index < 0 || index >= m_ContextItems.Count - 1) { return; }

            DS_HazeContextItem tmp = m_ContextItems[index];
            m_ContextItems.RemoveAt(index);
            m_ContextItems.Insert(index + 1, tmp);

            // Update the solo index.
            if (m_SoloItem == -1) { return; }
            if (m_SoloItem == index) { m_SoloItem++; }
            else if (m_SoloItem == index + 1) { m_SoloItem -= 1; }
        }

        /// <summary>
        /// Return a DS_HazeContextVariant that has the linearly interpolated values, from top-to-bottom
        /// in the stack. If a variant is 'soloed', or there's only one, then that is returned as-is.
        /// </summary>
        public DS_HazeContextItem GetContextItemBlended(float time = -1)
        {
            DS_HazeContextItem blended = new DS_HazeContextItem();
            blended.CopyFrom(m_ContextItems[0]);

            // If there's only the default variant, return it directly.
            if (m_ContextItems.Count == 1) { return blended; }

            // Check for a 'soloed' variant (editor only).
#if UNITY_EDITOR
            if (m_SoloItem > -1 && m_SoloItem < m_ContextItems.Count)
            {
                blended.CopyFrom(m_ContextItems[m_SoloItem]);
                return blended;
            }
#endif
            // Created a blended variant.
            time = Mathf.Clamp01(time);
            float weight = 0;
            for (int cv = 1; cv < m_ContextItems.Count; cv++)
            {
                weight = m_ContextItems[cv].m_Weight.Evaluate(time);
                blended.Lerp(m_ContextItems[cv], weight);
            }
                        
            return blended;
        }

        /// <summary>
        /// Return a specific time-of-day variant.
        /// </summary>
        /// <param name="index"> List index of the variant. </param>
        /// <returns></returns>
        public DS_HazeContextItem GetItemAtIndex(int index)
        {
            if (index < 0 || index >= m_ContextItems.Count) return null;

            return m_ContextItems[index];
        }

        /// <summary>
        /// Copy settings and time-of-day variants from another DS_HazeContext.
        /// </summary>
        /// <param name="other"> DS_HazeContext to copy from. </param>
        public void CopyFrom(DS_HazeContext other)
        {
            // Clear any existing variants first.
            if (m_ContextItems.Count > 0)
            {
                m_ContextItems.Clear();
            }

            for (int cv = 0; cv < other.m_ContextItems.Count; cv++)
            {
                DS_HazeContextItem ctxV = new DS_HazeContextItem();
                ctxV.CopyFrom(other.m_ContextItems[cv]);

                m_ContextItems.Add(ctxV);
            }
        }

        /// <summary>
        /// Create a DS_HazeScriptableContext from this context that can then
        /// be saved using Unity's asset pipeline.
        /// </summary>
        /// <returns> A new DS_HazeScriptableContext </returns>
        public DS_HazeContextAsset GetContextAsset()
        {
            DS_HazeContextAsset cxt = ScriptableObject.CreateInstance<DS_HazeContextAsset>();

            cxt.Context.CopyFrom(this);
            cxt.Context.m_SoloItem = -1;
            return cxt;
        }

        /// <summary>
        /// Get all the time-of-day variants' names in this context.
        /// </summary>
        /// <returns> String array of variant names. </returns>
        public string[] GetItemNames()
        {
            string[] names = new string[m_ContextItems.Count];

            for (int cv = 0; cv < m_ContextItems.Count; cv++)
            {
                names[cv] = m_ContextItems[cv].m_Name;
            }

            return names;
        }
    }
}
