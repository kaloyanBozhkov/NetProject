![alt text](https://raw.githubusercontent.com/kaloyanBozhkov/NetProject/master/LogicMap.jpg)

<h1>GROUP PROJECT</h1>
<p>This was my contribution to a group project during the second year in university.
<br/>
The main idea is that a central system (PrivateINet) can send commands to a device (ManufacturerDevice) on the ethernet physical layer (ISO/OSI). The central system is controlled entirely through master commands sent as UDP packets through localhost (SendUdpPacketsToPort). The fun part of this project was playing around with UDP packets going through a port on localhsot as well as sending and reading Ethernet packets travelling between two specific network interfaces on the physical layer, controlled with the Pcap.Net library.  
  
<h3>Written in C#</h3>
<h4>Sending a command</h4>

![alt text](https://raw.githubusercontent.com/kaloyanBozhkov/NetProject/master/SendCommandExample.jpg)

<h4>Receiving a command</h4>

![alt text](https://raw.githubusercontent.com/kaloyanBozhkov/NetProject/master/ExampleOfManufacturerDeviceReceivingPackets.png)

</p>
