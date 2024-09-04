# UnityReferenceFinder
Unity资源引用查找工具，包括依赖和被依赖信息
## 说明
1. 参考 https://github.com/blueberryzzz/ReferenceFinder 实现了一个高效的查找资源文件的依赖信息的工具
2. xx的依赖，指的是：该文件依赖的文件（即该文件引用了哪些文件）
3. xxx的被依赖，指的是：哪些文件依赖该文件（即哪些文件引用了该文件）
4. Unity 的右键 Select Dependencies 指的是（1），没有实现（2）
5. Unity2022 提供了右键 Find Reference In Project 功能，但没有卵用
6. 所有的引用信息（依赖和被依赖）缓存在 Library/ReferenceFinderCache 中，可手动刷新，除首次建立引用信息外，查找非常高效
7. 支持树形展示和平铺展示
8. 树形展示时，防止出现嵌套（A依赖B，B依赖C，C依赖A 的情况）时造成死循环，出现嵌套时不再向下添加子节点
9. 在平铺展示时，递归查找所有引用（例如：依赖的依赖，全部递归查找出来）

