import struct

class State(object):
  
    def Hash(self,db,txId):
        b = struct.pack(">I", len(db)) + db.encode('utf8')
        b += struct.pack(">Q", txId)
        #b += struct.pack(">Q", txId) #+ txHash
        print('afadsfasd')
        print(b)
        return b



