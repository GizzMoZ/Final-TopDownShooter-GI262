using UnityEngine;

public class KeyItem : MonoBehaviour  // A key on the floor. The player picks it up by touching it.
{
    [SerializeField] string keyId = "red";   // must match the door's Required Key Id exactly
    [SerializeField] Color keyColor = new Color(1f, 0.25f, 0.25f);
    [SerializeField] float bobHeight = 0.08f;
    [SerializeField] float bobSpeed = 3f;

    Vector3 startPos;

    void Awake()
    {
        startPos = transform.position;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = keyColor;

        var col = GetComponent<Collider2D>();
        if (col == null) Debug.LogWarning(name + ": add a Collider2D (a CircleCollider2D works) so the key can be picked up.", this);
        else col.isTrigger = true;
    }

    // a small floating so it stands out
    void Update()
    {
        transform.position = startPos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerHealth>() == null) return;

        PlayerKeys.Add(keyId);
        Destroy(gameObject);
    }
}