namespace ServerSkills;

public static class ItemFactory
{
    public static Item Create(CreateItemRequest request)
    {
        return new Item()
        {
            ObjectId = request.ObjectId,
            ObjectType = GameObjectType.ITEM,
            Name = request.Name,
            Count = request.Count,
        };
    }
}

// 1. 아이템을 만든다. (근데 여기서 하드코딩으로 만들지말고 외부에서 넣어주는 값으로 만드는 걸로
// 2. 각 GameRoom에 넣어주고 Broadcast 한다.
// 3. 클라에서 pick 시도를 한다.
// 4. 서버에서 각 룸마다 처음으로 들어온 값으로 처리 (동시성 처리하지 말고 오류? 나게끔)
// 5. 누가 먹었는지 로그
// 6. 후에 동시성을 처리하여 중복을 없애본다.