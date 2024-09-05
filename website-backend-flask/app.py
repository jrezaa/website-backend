from flask import Flask, request

app = Flask(__name__)

stores = {"My Store": {"items": [{"name": "Chair", "price": 15.99}]}}


@app.get("/store")
def get_stores():
    return {"stores": stores}


@app.post("/store")
def create_store():
    request_data = request.get_json()
    if not "name" in request_data:
        return {"message": "Request needs 'name' attribute"}, 400
    new_store = {"items": []}
    stores[request_data["name"]] = new_store
    return new_store, 201


@app.post("/store/<string:name>/item")
def create_item(name: str):
    request_data = request.get_json()
    if not "name" in request_data:
        return {"message": "Request needs 'name' attribute"}, 400
    if not "price" in request_data:
        return {"message": "Request needs 'price' attribute"}, 400
    if not name in stores:
        return {"message": f'Store with name "{name}" is not found.'}, 404
    stores[name]["items"].append(
        {"name": request_data["name"], "price": request_data["price"]}
    )
    return stores[name], 201


@app.get("/store/<string:name>")
def get_store(name: str):
    if not name in stores:
        return {"message": f"Store with name '{name}' is not found."}, 404
    return stores[name], 200
