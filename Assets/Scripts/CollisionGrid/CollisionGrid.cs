using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class CollisionGrid : MonoBehaviour
{
    [SerializeField, Range(0, 1)]
    float _attackTime;
    [SerializeField, Range(0, 1)]
    float _releaseTime;
    [SerializeField]
    Renderer _renderer;

    class CollisionInfo
    {
        public float power;
        public List<Vector3> points;
        public bool enabled;

        public CollisionInfo()
        {
            this.power = 0;
            this.enabled = true;
            this.points = new List<Vector3>();
        }
    }
    Dictionary<Collider, CollisionInfo> _collisions = new Dictionary<Collider, CollisionInfo>();
    const int MAX_COLLISION_POINTS = 10;
    Vector4[] _collisionPoints = new Vector4[MAX_COLLISION_POINTS];
    List<Collider> _collidersToRemove = new List<Collider>();

    int _CollisionPointsId;
    int _CollisionPointsLengthId;

    void Awake()
    {
        _CollisionPointsId = Shader.PropertyToID("_CollisionPoints");
        _CollisionPointsLengthId = Shader.PropertyToID("_CollisionPointsLength");
    }

    void Update()
    {
        _collidersToRemove.Clear();
        var dt = Time.deltaTime;
        var done = false;
        var count = 0;
        foreach (var pair in _collisions)
        {
            var collider = pair.Key;
            var info = pair.Value;
            var points = info.points;
            var power = info.power;

            power += info.enabled ? (dt / _attackTime) : (-dt / _releaseTime);
            if (power < 0)
            {
                _collidersToRemove.Add(collider);
                continue;
            }
            power = Mathf.Clamp01(power);
            info.power = power;

            if (done)
            {
                continue;
            }

            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                _collisionPoints[count] = new Vector4(point.x, point.y, point.z, power);
                count++;

                if (count >= MAX_COLLISION_POINTS)
                {
                    done = true;
                    break;
                }
            }
        }

        var material = _renderer.material;
        material.SetVectorArray(_CollisionPointsId, _collisionPoints);
        material.SetInt(_CollisionPointsLengthId, count);

        for (int i = 0; i < _collidersToRemove.Count; i++)
        {
            _collisions.Remove(_collidersToRemove[i]);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        var collider = collision.collider;
        CollisionInfo info;
        if (!_collisions.TryGetValue(collider, out info))
        {
            info = new CollisionInfo();
            _collisions[collider] = info;
        }

        info.enabled = true;
        info.points.Clear();
        for (int i = 0; i < collision.contacts.Length; i++)
        {
            var contact = collision.contacts[i];
            info.points.Add(contact.point);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        var collider = collision.collider;
        var info = _collisions[collider];
        info.points.Clear();
        for (int i = 0; i < collision.contacts.Length; i++)
        {
            var contact = collision.contacts[i];
            info.points.Add(contact.point);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        var collider = collision.collider;
        var info = _collisions[collider];
        info.enabled = false;
    }
}
