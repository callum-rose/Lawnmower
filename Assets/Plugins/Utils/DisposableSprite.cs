using UnityEngine;

namespace Utils
{
    public class DisposableSprite
    {
        private Sprite _sprite;
        public Sprite Sprite
        {
            get => _sprite;
            set
            {
                _sprite = value;
            }
        }

        public DisposableSprite()
        {
            Sprite = null;
        }

        public DisposableSprite(Sprite sprite)
        {
            Sprite = sprite;
        }

        public void Dispose()
        {
            if (Sprite != null)
            {
                UnityEngine.Object.Destroy(Sprite.texture);
                UnityEngine.Object.Destroy(Sprite);
            }
        }
    }
}
