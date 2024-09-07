from flask import Flask, request
from flask_smorest import abort
from db import items, stores
import uuid

app = Flask(__name__)


@app.get("/store")
def get_stores():
    return {"stores": list(stores.values())}


@app.post("/store")
def create_store():
    store_data = request.get_json()
    store_id = uuid.uuid4().hex
    if not "name" in store_data:
        abort(400, message="Request needs 'name' attribute.")
    if foundDuplicateStore(store_data, stores):
        abort(400, message=f"Store '{store_data["name"]}', already exists")
    store = {**store_data, "id": store_id}
    stores[store_id] = store
    return store, 201

def foundDuplicateStore(new_store, stores):
    for store in stores:
        if new_store["name"] == store["name"]: return True
    return False

def foundDuplicate(new_item, items):

    for item in items:
        if new_item["name"] == item and new_item["store_id"] == item["store_id"]:
            return True

    return False


@app.post("/item")
def create_item():
    item_data = request.get_json()
    if (
        "price" not in item_data
        or "store_id" not in item_data
        or "name" not in item_data
    ):
        abort(
            400,
            message="Bad Request. Ensure 'price', 'store_id' and 'name' are inclued in JSON payload.",
        )

    if foundDuplicate(item=item_data, items=items):
        abort(400, message=f"Item already exists")
    if item_data["store_id"] not in stores:
        abort(404, message="Store not found")

    item_id = uuid.uuid4().hex
    item = {**item_data, "id": item_id}
    items[item_id] = item

    return item, 201


@app.get("/item")
def get_all_items():
    return {"items": list(items.values())}


@app.get("/store/<string:store_id>")
def get_store(store_id: str):
    try:
        return stores[store_id], 200
    except KeyError:
        abort(404, message="Store not found.")


@app.get("/item/<string:item_id>")
def get_item(item_id):
    try:
        return items[item_id], 200
    except KeyError:
        abort(404, message="Item not found.")
