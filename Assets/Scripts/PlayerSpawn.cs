using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    [SerializeField] Transform player;

    void Awake()
    {
        player.position = transform.position;
        player.rotation = transform.rotation;
    }

    // Draws a green circle in the Scene view so you can see the spawn point
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
}