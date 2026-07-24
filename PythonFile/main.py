#!/usr/bin/env python3
import sys
from pyfanuc import pyfanuc

ipaddr = sys.argv[1]
program = sys.argv[2]

conn = pyfanuc(ip=ipaddr, port=8193)
try:
    if conn.connect():
        getfile = conn.getprog(program)
        
        if getfile and getfile != -1:
            # 🎯 วิธีที่ 1: ตัดบรรทัดว่างเปล่าที่ซ้ำซ้อนออก (\r, \n ที่เกินมา) แต่ยังคงแยกทีละบรรทัดสวยงาม
            lines = [line.strip() for line in getfile.splitlines() if line.strip()]
            clean_gcode = "\n".join(lines)
            
            # สั่ง print ข้อความดิบออกไปตรงๆ (ห้ามใช้ pprint) เพื่อให้ C# ดึงไปใช้งานต่อได้ทันที
            print(clean_gcode)
            
            # 💡 (ทางเลือก) วิธีที่ 2: ถ้าอยากให้ยุบเหลือแถวเดียวยาวๆ ไม่มีเว้นบรรทัดเลย ให้ใช้บรรทัดนี้แทน:
            # print(" ".join(lines))
        else:
            print("ERROR: ไม่พบโปรแกรม G-Code หรือเกิดข้อผิดพลาดในการดึงไฟล์")
            
    if conn.connected:
        conn.disconnect()
except Exception as e:
    print(f"ERROR: {e}")