using UnityEngine;
using UnityEngine.EventSystems;

namespace KrunchyStickmanMod.UIComponents {

    public class GUIDragHandler : MonoBehaviour, IDragHandler, IBeginDragHandler {
        private Vector2 lastMousePosition;

        public bool IsDraggable {
            get;
            set {
                field = value;
                enabled = field;
            }
        } = true;

        public void OnBeginDrag(PointerEventData eventData) {
            if (!IsDraggable)
                return;
            lastMousePosition = eventData.position;
        }

        public void OnDrag(PointerEventData eventData) {
            if (!IsDraggable)
                return;
            Vector2 currentMousePosition = eventData.position;
            Vector2 diff = currentMousePosition - lastMousePosition;
            GetComponent<RectTransform>().anchoredPosition += diff;
            lastMousePosition = currentMousePosition;
        }
    }
}