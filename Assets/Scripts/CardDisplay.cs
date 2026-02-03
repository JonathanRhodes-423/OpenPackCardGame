using UnityEngine;
using System.Collections;

public class CardDisplay : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propBlock;

    // We move the setup logic to a private method we can call safely
    private void EnsureSetup()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (propBlock == null) propBlock = new MaterialPropertyBlock();
    }

    public void ApplyData(CardData data)
    {
        EnsureSetup(); // Make sure components are linked before using them

        if (data == null)
        {
            Debug.LogError("CardDisplay: Received null CardData!");
            return;
        }

        if (data.artwork != null)
        {
            meshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetTexture("_Image_Front", data.artwork.texture);
            meshRenderer.SetPropertyBlock(propBlock);
        }

        gameObject.name = "Card_" + data.cardName;
    }

    // This is the method the error was looking for!
    public void Reveal(Transform targetPos)
    {
        StartCoroutine(RevealRoutine(targetPos));
    }

    IEnumerator RevealRoutine(Transform target)
    {
        float duration = 0.5f; // Half a second to fly out
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;

            // Move and Rotate toward the "Inspect" point
            transform.position = Vector3.Lerp(startPos, target.position, percent);
            transform.rotation = Quaternion.Slerp(startRot, target.rotation, percent);

            yield return null;
        }
    }
}