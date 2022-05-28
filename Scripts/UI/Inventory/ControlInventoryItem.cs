namespace GodotModules
{
    public class ControlInventoryItem : Control
    {
        [Export] protected readonly NodePath NodePathItem;
        [Export] protected readonly NodePath NodePathStackSize;

        private Node _itemParent;
        private Label _stackSize;

        private ControlInventory _inventory;
        private InventoryItem _item;

        private Vector2 _invItemSize = new Vector2(50, 50);
        private Vector2 _itemSize = new Vector2(20, 20);

        public override void _Ready()
        {
            _itemParent = GetNode<Node>(NodePathItem);
            _stackSize = GetNode<Label>(NodePathStackSize);
        }

        public void Init(ControlInventory inventory)
        {
            _inventory = inventory;
        }

        public void SetItem(InventoryItem item)
        {
            _item = item;

            if (_item.Type == InventoryItemType.Static)
                SetSprite(_item.Name);
            else if (_item.Type == InventoryItemType.Animated)
                SetAnimatedSprite(_item.Name);

            _stackSize.Text = $"{_item.StackSize}";
        }

        private void _on_Item_gui_input(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseButton)
            {
                if (mouseButton.ButtonIndex == (int)ButtonList.Left && mouseButton.Pressed)
                {
                    if (_inventory.HoldingItem)
                        return;

                    ClearItem();

                    if (_item.Type == InventoryItemType.Static)
                        _inventory.HoldItem(InitSprite(_item.Name));
                    else if (_item.Type == InventoryItemType.Animated)
                        _inventory.HoldItem(InitAnimatedSprite(_item.Name));

                    //_textureRect.Texture = null;
                    //var item = Prefabs.InventoryItemCursor.Instance<InventoryItemCursor>();
                    //_inventory.HoldItem(item);
                }
            }
        }

        private void SetSprite(string name)
        {
            ClearItem();

            var sprite = InitSprite(name);
            _itemParent.AddChild(sprite);
        }

        private Sprite InitSprite(string name)
        {
            var sprite = new Sprite();
            sprite.Texture = Items.Sprites[name];
            sprite.Position += _invItemSize / 2;
            sprite.Scale = _itemSize / sprite.Texture.GetSize();
            return sprite;
        }

        private void SetAnimatedSprite(string name)
        {
            ClearItem();

            var animatedSprite = InitAnimatedSprite(name);
            _itemParent.AddChild(animatedSprite);
        }

        private AnimatedSprite InitAnimatedSprite(string name)
        {
            var animatedSprite = new AnimatedSprite();
            animatedSprite.Frames = Items.AnimatedSprites[name];
            animatedSprite.Playing = true;
            animatedSprite.Position += _invItemSize / 2;
            animatedSprite.Scale = _itemSize / animatedSprite.Frames.GetFrame("default", 0).GetSize();
            return animatedSprite;
        }

        private void ClearItem()
        {
            foreach (Node child in _itemParent.GetChildren())
                child.QueueFree();
        }
    }
}
