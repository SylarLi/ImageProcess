public class Vector<T>
{
    private int mDimention;

    private T[] mData;

    public Vector(int dimention)
    {
        mDimention = dimention;
        mData = new T[mDimention];
    }

    public Vector(T[] data)
    {
        mDimention = data.Length;
        mData = data;
    }

    public int dimention
    {
        get
        {
            return mDimention;
        }
    }

    public T this[int index]
    {
        get
        {
            return mData[index];
        }
        set
        {
            mData[index] = value;
        }
    }
}